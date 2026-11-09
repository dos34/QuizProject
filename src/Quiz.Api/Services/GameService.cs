using System.Security.Claims;
using MassTransit;
using MongoDB.Driver;
using Quiz.Api.Dtos;
using Quiz.Api.Errors;
using Quiz.Contracts;
using Quiz.Contracts.Commands;
using Quiz.Contracts.Events;
using Quiz.Domain.Entities;
using Quiz.Domain.Rules;
using Quiz.Infrastructure.Auth;
using Quiz.Infrastructure.Mongo;

namespace Quiz.Api.Services;

/// <summary>
/// Всё, что меняет состояние игры со стороны HTTP. API только переводит игру Lobby → Running
/// и принимает ответы; дальше игрой управляет Worker по событиям из RabbitMQ.
/// </summary>
public sealed class GameService(
    MongoContext db,
    QuizService quizzes,
    JwtTokenService tokens,
    IPublishEndpoint bus,
    GameEventLog journal,
    TimeProvider time,
    ILogger<GameService> logger)
{
    private const int PinAttempts = 10;
    private const string Source = "api";

    public async Task<CreateGameResponse> CreateAsync(Guid hostId, Guid quizId, CancellationToken ct)
    {
        var quiz = await quizzes.LoadOwnedAsync(hostId, quizId, ct);
        if (quiz.Questions.Count == 0)
        {
            throw new ConflictException("Quiz has no questions.");
        }

        // PIN уникален среди активных игр (partial unique index). При коллизии просто пробуем другой.
        for (var attempt = 0; attempt < PinAttempts; attempt++)
        {
            var now = time.GetUtcNow().UtcDateTime;
            var game = new GameSession
            {
                QuizId = quiz.Id,
                HostId = hostId,
                QuizTitle = quiz.Title,
                Pin = PinGenerator.Generate(),
                Questions = quiz.Questions,
                CreatedAt = now,
                LastUpdated = now,
            };

            try
            {
                await db.Games.InsertOneAsync(game, cancellationToken: ct);
                logger.LogInformation("Game {GameId} created with PIN {Pin} for quiz {QuizId}", game.Id, game.Pin, quiz.Id);
                return new CreateGameResponse(game.Id, game.Pin);
            }
            catch (MongoWriteException e) when (e.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                logger.LogDebug("PIN collision, retrying");
            }
        }

        throw new ConflictException("Could not allocate a PIN, try again.");
    }

    public async Task<IReadOnlyList<GameSummary>> ListForHostAsync(Guid hostId, CancellationToken ct)
    {
        var games = await db.Games
            .Find(g => g.HostId == hostId)
            .SortByDescending(g => g.CreatedAt)
            .Limit(100)
            .ToListAsync(ct);

        return games
            .Select(g => new GameSummary(g.Id, g.QuizTitle, g.Pin, g.Status, g.Questions.Count, g.CreatedAt, g.FinishedAt))
            .ToList();
    }

    public async Task<GameStateResponse> GetStateAsync(Guid gameId, ClaimsPrincipal user, CancellationToken ct)
    {
        var game = await LoadVisibleAsync(gameId, user, ct);
        var playersCount = (int)await db.Players.CountDocumentsAsync(p => p.GameId == gameId, cancellationToken: ct);

        QuestionView? question = null;
        if (game.CurrentQuestion is { } q && game.Phase != QuestionPhase.None)
        {
            question = new QuestionView(
                game.CurrentQuestionIndex,
                q.Text,
                q.Options,
                q.TimeLimitSeconds,
                game.QuestionStartedAt,
                game.QuestionDeadline,
                game.Phase == QuestionPhase.Closed ? q.CorrectOptionIndex : null,
                q.ImageUrl);
        }

        bool? answered = null;
        if (user.IsPlayer() && game.Phase != QuestionPhase.None)
        {
            var playerId = user.GetSubjectId();
            var index = game.CurrentQuestionIndex;
            answered = await db.Answers
                .Find(a => a.GameId == gameId && a.QuestionIndex == index && a.PlayerId == playerId)
                .AnyAsync(ct);
        }

        return new GameStateResponse(
            game.Id,
            game.QuizTitle,
            game.Pin,
            game.Status,
            game.Phase,
            game.CurrentQuestionIndex,
            game.Questions.Count,
            question,
            playersCount,
            answered,
            time.GetUtcNow().UtcDateTime);
    }

    public async Task StartAsync(Guid hostId, Guid gameId, CancellationToken ct)
    {
        var game = await LoadOwnedAsync(hostId, gameId, ct);
        if (game.Status != GameStatus.Lobby)
        {
            throw new ConflictException($"Game is already {game.Status}.");
        }

        if (!await db.Players.Find(p => p.GameId == gameId).AnyAsync(ct))
        {
            throw new ConflictException("Nobody has joined the game yet.");
        }

        var now = time.GetUtcNow().UtcDateTime;

        // Условный апдейт: при двойном клике «Старт» GameStarted опубликуется ровно один раз.
        var result = await db.Games.UpdateOneAsync(
            g => g.Id == gameId && g.Status == GameStatus.Lobby,
            Builders<GameSession>.Update
                .Set(g => g.Status, GameStatus.Running)
                .Set(g => g.StartedAt, now)
                .Set(g => g.LastUpdated, now),
            cancellationToken: ct);

        if (result.ModifiedCount == 0)
        {
            throw new ConflictException("Game has already been started.");
        }

        var message = new GameStarted(gameId, now);
        await bus.Publish(message, ct);
        await journal.WriteAsync(gameId, Source, message, ct);
        logger.LogInformation("Game {GameId} started", gameId);
    }

    public async Task NextAsync(Guid hostId, Guid gameId, CancellationToken ct)
    {
        var game = await LoadOwnedAsync(hostId, gameId, ct);
        if (game.Status != GameStatus.Running)
        {
            throw new ConflictException($"Game is {game.Status}.");
        }

        switch (game.Phase)
        {
            case QuestionPhase.Closed:
                await bus.Publish(new AdvanceGame(gameId, game.CurrentQuestionIndex), ct);
                break;
            case QuestionPhase.Open:
                // «Далее» во время открытого вопроса = закрыть досрочно. Worker закроет идемпотентно.
                await bus.Publish(new CloseQuestion(gameId, game.CurrentQuestionIndex), ct);
                break;
            default:
                throw new ConflictException("The first question has not been shown yet.");
        }
    }

    public async Task<JoinGameResponse> JoinAsync(JoinGameRequest request, CancellationToken ct)
    {
        var pin = request.Pin.Trim();
        var nickname = request.Nickname.Trim();
        if (!PinGenerator.IsValid(pin))
        {
            throw new ValidationException(["PIN must be 6 digits."]);
        }

        if (nickname.Length == 0)
        {
            throw new ValidationException(["Nickname must not be empty."]);
        }

        var game = await db.Games.Find(g => g.Pin == pin && g.IsActive).FirstOrDefaultAsync(ct)
                   ?? throw new NotFoundException("Game with this PIN is not found.");

        var player = new Player
        {
            GameId = game.Id,
            Nickname = nickname,
            NicknameKey = nickname.ToLowerInvariant(),
            JoinedAt = time.GetUtcNow().UtcDateTime,
        };

        try
        {
            await db.Players.InsertOneAsync(player, cancellationToken: ct);
        }
        catch (MongoWriteException e) when (e.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new ConflictException("This nickname is already taken in the game.");
        }

        var count = (int)await db.Players.CountDocumentsAsync(p => p.GameId == game.Id, cancellationToken: ct);
        var message = new PlayerJoined(game.Id, player.Id, player.Nickname, count);
        await bus.Publish(message, ct);
        await journal.WriteAsync(game.Id, Source, message, ct);

        var (token, _) = tokens.IssuePlayerToken(player.Id, game.Id, player.Nickname);
        return new JoinGameResponse(game.Id, player.Id, player.Nickname, token);
    }

    public async Task<SubmitAnswerResponse> SubmitAnswerAsync(
        Guid gameId, ClaimsPrincipal user, SubmitAnswerRequest request, CancellationToken ct)
    {
        // Время фиксируем сразу при входе в метод — до любых запросов в БД.
        var now = time.GetUtcNow().UtcDateTime;
        var playerId = user.GetSubjectId();
        if (user.GetGameId() != gameId)
        {
            throw new ForbiddenException("Player token belongs to another game.");
        }

        var game = await db.Games.Find(g => g.Id == gameId).FirstOrDefaultAsync(ct)
                   ?? throw new NotFoundException("Game not found.");

        var rejection = AnswerRules.Check(game, request.QuestionIndex, request.OptionIndex, now);
        if (rejection != AnswerRejection.None)
        {
            throw rejection == AnswerRejection.InvalidOption
                ? new ValidationException(["Option index is out of range."])
                : new ConflictException($"Answer rejected: {rejection}.");
        }

        var elapsedMs = (int)Math.Max(0, (now - game.QuestionStartedAt!.Value).TotalMilliseconds);
        var answer = new Answer
        {
            GameId = gameId,
            QuestionIndex = request.QuestionIndex,
            PlayerId = playerId,
            OptionIndex = request.OptionIndex,
            SubmittedAt = now,
            ElapsedMs = elapsedMs,
        };

        try
        {
            await db.Answers.InsertOneAsync(answer, cancellationToken: ct);
        }
        catch (MongoWriteException e) when (e.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new ConflictException("You have already answered this question.");
        }

        var questionIndex = request.QuestionIndex;
        var answered = (int)await db.Answers.CountDocumentsAsync(
            a => a.GameId == gameId && a.QuestionIndex == questionIndex, cancellationToken: ct);
        var players = (int)await db.Players.CountDocumentsAsync(p => p.GameId == gameId, cancellationToken: ct);

        await bus.Publish(new AnswerSubmitted(gameId, questionIndex, playerId, answered, players), ct);
        return new SubmitAnswerResponse(true, elapsedMs);
    }

    public async Task<IReadOnlyList<LeaderboardEntry>> GetLeaderboardAsync(Guid gameId, ClaimsPrincipal user, CancellationToken ct)
    {
        await LoadVisibleAsync(gameId, user, ct);
        var players = await db.Players.Find(p => p.GameId == gameId).ToListAsync(ct);
        return Leaderboard.Build(players);
    }

    private async Task<GameSession> LoadOwnedAsync(Guid hostId, Guid gameId, CancellationToken ct)
    {
        var game = await db.Games.Find(g => g.Id == gameId && g.HostId == hostId).FirstOrDefaultAsync(ct);
        return game ?? throw new NotFoundException("Game not found.");
    }

    /// <summary>Игру видят её ведущий и игроки этой игры.</summary>
    private async Task<GameSession> LoadVisibleAsync(Guid gameId, ClaimsPrincipal user, CancellationToken ct)
    {
        var game = await db.Games.Find(g => g.Id == gameId).FirstOrDefaultAsync(ct)
                   ?? throw new NotFoundException("Game not found.");

        var allowed = (user.IsHost() && game.HostId == user.GetSubjectId())
                      || (user.IsPlayer() && user.GetGameId() == gameId);

        return allowed ? game : throw new NotFoundException("Game not found.");
    }
}
