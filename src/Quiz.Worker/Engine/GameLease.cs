using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Quiz.Domain.Entities;
using Quiz.Infrastructure.Messaging;
using Quiz.Infrastructure.Mongo;

namespace Quiz.Worker.Engine;

/// <summary>Игру сейчас обрабатывает кто-то другой. MassTransit повторит сообщение позже.</summary>
public sealed class LeaseBusyException(Guid gameId) : Exception($"Game {gameId} is leased by another worker");

/// <summary>
/// Lease-блокировка игры в Mongo: в каждый момент шаг игры выполняет только один обработчик
/// (даже при нескольких репликах воркера и параллельных consumer'ах). Если воркер упал,
/// lease истекает по LeaseUntil и игру подхватывает другой.
/// </summary>
public sealed class GameLease(MongoContext db, TimeProvider time, IOptions<WorkerOptions> options)
{
    private static readonly string Instance = $"{Environment.MachineName}:{Environment.ProcessId}";

    public async Task<Handle> AcquireAsync(Guid gameId, CancellationToken ct)
    {
        var owner = $"{Instance}:{Guid.NewGuid():N}";
        var now = time.GetUtcNow().UtcDateTime;

        var filter = Builders<GameSession>.Filter.And(
            Builders<GameSession>.Filter.Eq(g => g.Id, gameId),
            Builders<GameSession>.Filter.Or(
                Builders<GameSession>.Filter.Eq(g => g.LeaseUntil, null),
                Builders<GameSession>.Filter.Lt(g => g.LeaseUntil, now)));

        var update = Builders<GameSession>.Update
            .Set(g => g.LeaseOwner, owner)
            .Set(g => g.LeaseUntil, now.Add(options.Value.LeaseTtl))
            .Inc(g => g.Attempts, 1);

        var result = await db.Games.UpdateOneAsync(filter, update, cancellationToken: ct);
        if (result.MatchedCount == 1)
        {
            return new Handle(db, gameId, owner);
        }

        var exists = await db.Games.Find(g => g.Id == gameId).AnyAsync(ct);
        throw exists ? new LeaseBusyException(gameId) : new PoisonMessageException($"Game {gameId} does not exist");
    }

    public sealed class Handle(MongoContext db, Guid gameId, string owner) : IAsyncDisposable
    {
        public string Owner => owner;

        public async ValueTask DisposeAsync()
        {
            // Снимаем только свой lease. Если он уже истёк и его взял другой — ничего не трогаем.
            await db.Games.UpdateOneAsync(
                g => g.Id == gameId && g.LeaseOwner == owner,
                Builders<GameSession>.Update.Set(g => g.LeaseOwner, null).Set(g => g.LeaseUntil, null));
        }
    }
}
