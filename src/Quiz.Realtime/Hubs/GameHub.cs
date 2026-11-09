using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using Quiz.Infrastructure.Auth;
using Quiz.Infrastructure.Mongo;

namespace Quiz.Realtime.Hubs;

/// <summary>
/// Клиент подключается с JWT (ведущего или игрока) и вызывает JoinGame(gameId).
/// После переподключения клиент снова вызывает JoinGame и забирает текущее состояние через GET /api/games/{id}.
/// </summary>
[Authorize]
public sealed class GameHub(MongoContext db, ILogger<GameHub> logger) : Hub<IGameClient>
{
    public async Task JoinGame(Guid gameId)
    {
        var user = Context.User ?? throw new HubException("Unauthorized");

        if (user.IsPlayer())
        {
            if (user.GetGameId() != gameId)
            {
                throw new HubException("Token belongs to another game");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, GameGroups.All(gameId));
            logger.LogInformation("Player {PlayerId} subscribed to game {GameId}", user.GetSubjectId(), gameId);
            return;
        }

        if (user.IsHost())
        {
            var hostId = user.GetSubjectId();
            var owns = await db.Games.Find(g => g.Id == gameId && g.HostId == hostId).AnyAsync();
            if (!owns)
            {
                throw new HubException("Game not found");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, GameGroups.All(gameId));
            await Groups.AddToGroupAsync(Context.ConnectionId, GameGroups.Host(gameId));
            logger.LogInformation("Host {HostId} subscribed to game {GameId}", hostId, gameId);
            return;
        }

        throw new HubException("Unknown role");
    }

    public async Task LeaveGame(Guid gameId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GameGroups.All(gameId));
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GameGroups.Host(gameId));
    }
}
