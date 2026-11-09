using System.Security.Claims;

namespace Quiz.Infrastructure.Auth;

public static class QuizClaims
{
    public const string Subject = "sub";
    public const string Role = "role";
    public const string GameId = "game_id";
    public const string Nickname = "nickname";

    public const string HostRole = "host";
    public const string PlayerRole = "player";

    public const string HostPolicy = "HostOnly";
    public const string PlayerPolicy = "PlayerOnly";

    /// <summary>Для ведущего — id пользователя, для игрока — id игрока.</summary>
    public static Guid GetSubjectId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(Subject), out var id)
            ? id
            : throw new UnauthorizedAccessException("Token has no valid subject");

    public static Guid? GetGameId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(GameId), out var id) ? id : null;

    public static bool IsHost(this ClaimsPrincipal principal) => principal.HasClaim(Role, HostRole);

    public static bool IsPlayer(this ClaimsPrincipal principal) => principal.HasClaim(Role, PlayerRole);
}
