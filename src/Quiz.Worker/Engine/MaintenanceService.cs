using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Quiz.Domain.Entities;
using Quiz.Infrastructure.Mongo;

namespace Quiz.Worker.Engine;

/// <summary>
/// Периодические задачи воркера:
///  - восстановление зависших игр (воркер упал, отложенное сообщение потерялось и т.п.);
///  - пометка брошенных игр как Abandoned.
/// Безопасно запускать на нескольких репликах: все операции движка идемпотентны и защищены lease.
/// </summary>
public sealed class MaintenanceService(
    IServiceScopeFactory scopes,
    MongoContext db,
    TimeProvider time,
    IOptions<WorkerOptions> options,
    ILogger<MaintenanceService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.MaintenanceInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Maintenance iteration failed");
            }
        }
    }

    public async Task RunOnceAsync(CancellationToken ct)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var o = options.Value;

        await using var scope = scopes.CreateAsyncScope();
        var engine = scope.ServiceProvider.GetRequiredService<GameEngine>();

        var leaseFree = Builders<GameSession>.Filter.Or(
            Builders<GameSession>.Filter.Eq(g => g.LeaseUntil, null),
            Builders<GameSession>.Filter.Lt(g => g.LeaseUntil, now));

        // 1. Вопрос открыт, дедлайн давно прошёл, а CloseQuestion так и не обработан.
        var stuckQuestions = await db.Games.Find(Builders<GameSession>.Filter.And(
                Builders<GameSession>.Filter.Eq(g => g.Status, GameStatus.Running),
                Builders<GameSession>.Filter.Eq(g => g.Phase, QuestionPhase.Open),
                Builders<GameSession>.Filter.Lt(g => g.QuestionDeadline, now - o.StuckAfter),
                leaseFree))
            .Limit(50)
            .ToListAsync(ct);

        foreach (var game in stuckQuestions)
        {
            logger.LogWarning("Recovering stuck question {QuestionIndex} in game {GameId}", game.CurrentQuestionIndex, game.Id);
            await SafeAsync(() => engine.CloseQuestionAsync(game.Id, game.CurrentQuestionIndex, ct), game.Id);
        }

        // 2. Игра запущена, но первый вопрос так и не показан (потерялся GameStarted).
        var stuckStarts = await db.Games.Find(Builders<GameSession>.Filter.And(
                Builders<GameSession>.Filter.Eq(g => g.Status, GameStatus.Running),
                Builders<GameSession>.Filter.Eq(g => g.Phase, QuestionPhase.None),
                Builders<GameSession>.Filter.Lt(g => g.StartedAt, now - o.StuckAfter),
                leaseFree))
            .Limit(50)
            .ToListAsync(ct);

        foreach (var game in stuckStarts)
        {
            logger.LogWarning("Recovering not started game {GameId}", game.Id);
            await SafeAsync(() => engine.StartGameAsync(game.Id, ct), game.Id);
        }

        // 3. Брошенные игры: никто ничего не делал дольше AbandonAfter.
        var threshold = now - o.AbandonAfter;
        var abandoned = await db.Games
            .Find(g => g.IsActive && g.LastUpdated < threshold)
            .Project(g => g.Id)
            .Limit(100)
            .ToListAsync(ct);

        foreach (var gameId in abandoned)
        {
            if (await engine.AbandonAsync(gameId, threshold, ct))
            {
                logger.LogInformation("Game {GameId} marked as abandoned", gameId);
            }
        }
    }

    private async Task SafeAsync(Func<Task> action, Guid gameId)
    {
        try
        {
            await action();
        }
        catch (LeaseBusyException)
        {
            // кто-то уже занимается этой игрой
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to recover game {GameId}", gameId);
        }
    }
}
