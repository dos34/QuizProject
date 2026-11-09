using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Quiz.Domain.Entities;

namespace Quiz.Infrastructure.Mongo;

/// <summary>
/// Создаёт индексы при старте сервиса. createIndexes идемпотентен, поэтому безопасно
/// запускать из нескольких сервисов/реплик одновременно.
/// </summary>
public sealed class MongoIndexInitializer(MongoContext db, ILogger<MongoIndexInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await db.Users.Indexes.CreateOneAsync(new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(x => x.Login),
            new CreateIndexOptions { Unique = true, Name = "ux_login" }), cancellationToken: cancellationToken);

        await db.Quizzes.Indexes.CreateOneAsync(new CreateIndexModel<QuizEntity>(
            Builders<QuizEntity>.IndexKeys.Ascending(x => x.OwnerId).Descending(x => x.UpdatedAt),
            new CreateIndexOptions { Name = "ix_owner_updated" }), cancellationToken: cancellationToken);

        await db.Games.Indexes.CreateManyAsync(
        [
            // PIN уникален только среди активных игр: старые PIN можно переиспользовать.
            new CreateIndexModel<GameSession>(
                Builders<GameSession>.IndexKeys.Ascending(x => x.Pin),
                new CreateIndexOptions<GameSession>
                {
                    Unique = true,
                    Name = "ux_pin_active",
                    PartialFilterExpression = Builders<GameSession>.Filter.Eq(x => x.IsActive, true),
                }),
            // Поиск зависших игр воркером: Running + истёкший lease.
            new CreateIndexModel<GameSession>(
                Builders<GameSession>.IndexKeys.Ascending(x => x.Status).Ascending(x => x.LeaseUntil),
                new CreateIndexOptions { Name = "ix_status_lease" }),
            // История игр ведущего.
            new CreateIndexModel<GameSession>(
                Builders<GameSession>.IndexKeys.Ascending(x => x.HostId).Descending(x => x.CreatedAt),
                new CreateIndexOptions { Name = "ix_host_created" }),
        ], cancellationToken);

        await db.Players.Indexes.CreateOneAsync(new CreateIndexModel<Player>(
            Builders<Player>.IndexKeys.Ascending(x => x.GameId).Ascending(x => x.NicknameKey),
            new CreateIndexOptions { Unique = true, Name = "ux_game_nickname" }), cancellationToken: cancellationToken);

        // Игрок не может ответить на один вопрос дважды — идемпотентность на уровне БД.
        await db.Answers.Indexes.CreateOneAsync(new CreateIndexModel<Answer>(
            Builders<Answer>.IndexKeys.Ascending(x => x.GameId).Ascending(x => x.QuestionIndex).Ascending(x => x.PlayerId),
            new CreateIndexOptions { Unique = true, Name = "ux_game_question_player" }), cancellationToken: cancellationToken);

        await db.Events.Indexes.CreateOneAsync(new CreateIndexModel<GameEvent>(
            Builders<GameEvent>.IndexKeys.Ascending(x => x.GameId).Ascending(x => x.At),
            new CreateIndexOptions { Name = "ix_game_at" }), cancellationToken: cancellationToken);

        logger.LogInformation("Mongo indexes ensured");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
