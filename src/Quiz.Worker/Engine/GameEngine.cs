using MassTransit;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Quiz.Contracts.Commands;
using Quiz.Contracts.Events;
using Quiz.Domain.Entities;
using Quiz.Domain.Rules;
using Quiz.Infrastructure.Messaging;
using Quiz.Infrastructure.Mongo;

namespace Quiz.Worker.Engine;

/// <summary>
/// Конечный автомат игры:
/// Running/None → (вопрос N) Open → Closed → (AdvanceGame) Open N+1 → ... → Closed (последний) → Finished.
///
/// Каждый шаг:
///  1) берёт lease на игру;
///  2) проверяет, что игра в ожидаемом состоянии (иначе сообщение — дубль/ретрай, просто пропускаем);
///  3) меняет состояние условным апдейтом (compare-and-set по индексу вопроса и фазе);
///  4) только если переход реально произошёл — публикует события.
/// </summary>
public sealed class GameEngine(
    MongoContext db,
    GameLease lease,
    IPublishEndpoint publisher,
    IMessageScheduler scheduler,
    GameEventLog journal,
    TimeProvider time,
    IOptions<WorkerOptions> options,
    ILogger<GameEngine> logger)
{
    private const string Source = "worker";

    public async Task StartGameAsync(Guid gameId, CancellationToken ct)
    {
        await using var _ = await lease.AcquireAsync(gameId, ct);
        var game = await LoadAsync(gameId, ct);

        if (game.Status != GameStatus.Running || game.Phase != QuestionPhase.None)
        {
            logger.LogInformation("Game {GameId} already in progress ({Status}/{Phase}), skip start", gameId, game.Status, game.Phase);
            return;
        }

        await OpenQuestionAsync(game, 0, ct);
    }

    public async Task CloseQuestionAsync(Guid gameId, int questionIndex, CancellationToken ct)
    {
        await using var _ = await lease.AcquireAsync(gameId, ct);
        var game = await LoadAsync(gameId, ct);

        // Идемпотентность: повторный/опоздавший CloseQuestion для уже закрытого вопроса ничего не делает.
        if (game.Status != GameStatus.Running || game.CurrentQuestionIndex != questionIndex || game.Phase != QuestionPhase.Open)
        {
            logger.LogInformation(
                "CloseQuestion {QuestionIndex} for game {GameId} ignored: state is {Index}/{Phase}",
                questionIndex, gameId, game.CurrentQuestionIndex, game.Phase);
            return;
        }

        var question = game.Questions[questionIndex];
        var answers = await db.Answers.Find(a => a.GameId == gameId && a.QuestionIndex == questionIndex).ToListAsync(ct);

        // 1. Очки за каждый ответ. Set, а не Inc — повторный подсчёт даёт тот же результат.
        if (answers.Count > 0)
        {
            var writes = answers.Select(a =>
            {
                var correct = a.OptionIndex == question.CorrectOptionIndex;
                var points = Scoring.Calculate(correct, a.ElapsedMs, question.TimeLimitSeconds);
                return new UpdateOneModel<Answer>(
                    Builders<Answer>.Filter.Eq(x => x.Id, a.Id),
                    Builders<Answer>.Update.Set(x => x.IsCorrect, correct).Set(x => x.Points, points));
            });
            await db.Answers.BulkWriteAsync(writes, cancellationToken: ct);
        }

        // 2. Счёт игроков пересчитывается как сумма очков по всем их ответам — тоже идемпотентно.
        await RecalculateScoresAsync(gameId, ct);

        // 3. Переход Open → Closed. Если упадём до этой строки — ретрай повторит шаги 1–2 без вреда.
        var now = time.GetUtcNow().UtcDateTime;
        var transition = await db.Games.UpdateOneAsync(
            g => g.Id == gameId && g.CurrentQuestionIndex == questionIndex && g.Phase == QuestionPhase.Open,
            Builders<GameSession>.Update
                .Set(g => g.Phase, QuestionPhase.Closed)
                .Set(g => g.LastUpdated, now)
                .Set(g => g.Attempts, 0),
            cancellationToken: ct);

        if (transition.ModifiedCount == 0)
        {
            return;
        }

        var counts = new int[question.Options.Count];
        foreach (var a in answers.Where(a => a.OptionIndex >= 0 && a.OptionIndex < counts.Length))
        {
            counts[a.OptionIndex]++;
        }

        var closed = new QuestionClosed(gameId, questionIndex, question.CorrectOptionIndex, counts, game.IsLastQuestion);
        var leaderboard = new LeaderboardUpdated(gameId, questionIndex, await BuildLeaderboardAsync(gameId, ct));

        await publisher.Publish(closed, ct);
        await publisher.Publish(leaderboard, ct);
        await journal.WriteAsync(gameId, Source, closed, ct);

        logger.LogInformation(
            "Question {QuestionIndex} of game {GameId} closed: {Answers} answers, {Correct} correct",
            questionIndex, gameId, answers.Count, counts[question.CorrectOptionIndex]);
    }

    public async Task AdvanceAsync(Guid gameId, int expectedQuestionIndex, CancellationToken ct)
    {
        await using var _ = await lease.AcquireAsync(gameId, ct);
        var game = await LoadAsync(gameId, ct);

        if (game.Status != GameStatus.Running
            || game.CurrentQuestionIndex != expectedQuestionIndex
            || game.Phase != QuestionPhase.Closed)
        {
            logger.LogInformation("AdvanceGame for {GameId} from {Index} ignored (duplicate or stale)", gameId, expectedQuestionIndex);
            return;
        }

        if (game.IsLastQuestion)
        {
            await FinishAsync(game, ct);
        }
        else
        {
            await OpenQuestionAsync(game, expectedQuestionIndex + 1, ct);
        }
    }

    /// <summary>Пометить брошенной (вызывается фоновой задачей).</summary>
    public async Task<bool> AbandonAsync(Guid gameId, DateTime notUpdatedSince, CancellationToken ct)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var result = await db.Games.UpdateOneAsync(
            g => g.Id == gameId && g.IsActive && g.LastUpdated < notUpdatedSince,
            Builders<GameSession>.Update
                .Set(g => g.Status, GameStatus.Abandoned)
                .Set(g => g.IsActive, false)
                .Set(g => g.FinishedAt, now)
                .Set(g => g.LastUpdated, now),
            cancellationToken: ct);
        return result.ModifiedCount == 1;
    }

    private async Task OpenQuestionAsync(GameSession game, int index, CancellationToken ct)
    {
        var question = game.Questions[index];
        var now = time.GetUtcNow().UtcDateTime;
        var deadline = now.AddSeconds(question.TimeLimitSeconds);
        var prevIndex = game.CurrentQuestionIndex;
        var prevPhase = game.Phase;

        var transition = await db.Games.UpdateOneAsync(
            g => g.Id == game.Id
                 && g.Status == GameStatus.Running
                 && g.CurrentQuestionIndex == prevIndex
                 && g.Phase == prevPhase,
            Builders<GameSession>.Update
                .Set(g => g.CurrentQuestionIndex, index)
                .Set(g => g.Phase, QuestionPhase.Open)
                .Set(g => g.QuestionStartedAt, now)
                .Set(g => g.QuestionDeadline, deadline)
                .Set(g => g.LastUpdated, now)
                .Set(g => g.Attempts, 0),
            cancellationToken: ct);

        if (transition.ModifiedCount == 0)
        {
            return;
        }

        // Правильный ответ клиентам не отправляем — только после закрытия вопроса.
        var started = new QuestionStarted(
            game.Id, index, game.Questions.Count, question.Text, question.Options,
            question.TimeLimitSeconds, now, deadline);

        await publisher.Publish(started, ct);

        // Таймер вопроса — отложенное сообщение в RabbitMQ. Переживает рестарт воркера.
        await scheduler.SchedulePublish(deadline.Add(options.Value.CloseGrace), new CloseQuestion(game.Id, index), ct);
        await journal.WriteAsync(game.Id, Source, started, ct);

        logger.LogInformation("Game {GameId}: question {QuestionIndex} opened until {Deadline:O}", game.Id, index, deadline);
    }

    private async Task FinishAsync(GameSession game, CancellationToken ct)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var lastIndex = game.CurrentQuestionIndex;
        var transition = await db.Games.UpdateOneAsync(
            g => g.Id == game.Id && g.Status == GameStatus.Running && g.CurrentQuestionIndex == lastIndex,
            Builders<GameSession>.Update
                .Set(g => g.Status, GameStatus.Finished)
                .Set(g => g.IsActive, false)
                .Set(g => g.FinishedAt, now)
                .Set(g => g.LastUpdated, now),
            cancellationToken: ct);

        if (transition.ModifiedCount == 0)
        {
            return;
        }

        var finished = new GameFinished(game.Id, now, await BuildLeaderboardAsync(game.Id, ct));
        await publisher.Publish(finished, ct);
        await journal.WriteAsync(game.Id, Source, finished, ct);
        logger.LogInformation("Game {GameId} finished", game.Id);
    }

    private async Task RecalculateScoresAsync(Guid gameId, CancellationToken ct)
    {
        var totals = await db.Answers.Aggregate()
            .Match(a => a.GameId == gameId)
            .Group(a => a.PlayerId, g => new { PlayerId = g.Key, Total = g.Sum(x => x.Points) })
            .ToListAsync(ct);

        if (totals.Count == 0)
        {
            return;
        }

        var writes = totals.Select(t => new UpdateOneModel<Player>(
            Builders<Player>.Filter.Eq(p => p.Id, t.PlayerId),
            Builders<Player>.Update.Set(p => p.Score, t.Total)));
        await db.Players.BulkWriteAsync(writes, cancellationToken: ct);
    }

    private async Task<IReadOnlyList<Quiz.Contracts.LeaderboardEntry>> BuildLeaderboardAsync(Guid gameId, CancellationToken ct)
    {
        var players = await db.Players.Find(p => p.GameId == gameId).ToListAsync(ct);
        return Leaderboard.Build(players);
    }

    private async Task<GameSession> LoadAsync(Guid gameId, CancellationToken ct) =>
        await db.Games.Find(g => g.Id == gameId).FirstOrDefaultAsync(ct)
        ?? throw new PoisonMessageException($"Game {gameId} does not exist");
}
