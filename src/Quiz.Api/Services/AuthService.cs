using Microsoft.AspNetCore.Identity;
using MongoDB.Driver;
using Quiz.Api.Dtos;
using Quiz.Api.Errors;
using Quiz.Domain.Entities;
using Quiz.Infrastructure.Mongo;

namespace Quiz.Api.Services;

public sealed class AuthService(MongoContext db, JwtTokenService tokens, IPasswordHasher<User> hasher)
{
    public async Task<TokenResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var login = request.Login.Trim().ToLowerInvariant();
        var user = new User { Login = login, PasswordHash = "" };
        user.PasswordHash = hasher.HashPassword(user, request.Password);

        try
        {
            await db.Users.InsertOneAsync(user, cancellationToken: ct);
        }
        catch (MongoWriteException e) when (e.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new ConflictException("Login is already taken.");
        }

        return Token(user);
    }

    public async Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var login = request.Login.Trim().ToLowerInvariant();
        var user = await db.Users.Find(u => u.Login == login).FirstOrDefaultAsync(ct);

        // Одинаковая ошибка для «нет пользователя» и «неверный пароль» — не раскрываем, какие логины существуют.
        if (user is null || hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedException("Invalid login or password.");
        }

        return Token(user);
    }

    private TokenResponse Token(User user)
    {
        var (token, expires) = tokens.IssueHostToken(user.Id, user.Login);
        return new TokenResponse(token, expires, user.Id, user.Login);
    }
}
