using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Quiz.Infrastructure.Auth;

namespace Quiz.Api.Services;

/// <summary>Выпускает JWT: долгоживущий для ведущего и короткоживущий токен сессии для игрока.</summary>
public sealed class JwtTokenService(IOptions<JwtOptions> options, TimeProvider time)
{
    private readonly JwtOptions _jwt = options.Value;

    public (string Token, DateTime ExpiresAt) IssueHostToken(Guid userId, string login) =>
        Issue(
            [
                new Claim(QuizClaims.Subject, userId.ToString()),
                new Claim(QuizClaims.Role, QuizClaims.HostRole),
                new Claim("login", login),
            ],
            _jwt.HostTokenLifetime);

    public (string Token, DateTime ExpiresAt) IssuePlayerToken(Guid playerId, Guid gameId, string nickname) =>
        Issue(
            [
                new Claim(QuizClaims.Subject, playerId.ToString()),
                new Claim(QuizClaims.Role, QuizClaims.PlayerRole),
                new Claim(QuizClaims.GameId, gameId.ToString()),
                new Claim(QuizClaims.Nickname, nickname),
            ],
            _jwt.PlayerTokenLifetime);

    private (string, DateTime) Issue(IEnumerable<Claim> claims, TimeSpan lifetime)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var expires = now.Add(lifetime);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key)), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(_jwt.Issuer, _jwt.Audience, claims, now, expires, credentials);
        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
