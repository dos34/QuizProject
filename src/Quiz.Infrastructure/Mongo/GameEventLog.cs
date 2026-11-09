using System.Text.Json;
using Microsoft.Extensions.Logging;
using Quiz.Domain.Entities;

namespace Quiz.Infrastructure.Mongo;

/// <summary>Пишет журнал событий игры. Сбой журнала не должен ломать игру — только warning в лог.</summary>
public sealed class GameEventLog(MongoContext db, ILogger<GameEventLog> logger)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task WriteAsync<T>(Guid gameId, string source, T payload, CancellationToken ct = default)
    {
        try
        {
            await db.Events.InsertOneAsync(new GameEvent
            {
                GameId = gameId,
                Type = typeof(T).Name,
                Source = source,
                Payload = JsonSerializer.Serialize(payload, Json),
            }, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to write game event {EventType} for game {GameId}", typeof(T).Name, gameId);
        }
    }
}
