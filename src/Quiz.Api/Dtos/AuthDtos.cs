using System.ComponentModel.DataAnnotations;

namespace Quiz.Api.Dtos;

public sealed record RegisterRequest(
    [Required, StringLength(32, MinimumLength = 3), RegularExpression("^[a-zA-Z0-9_.-]+$")] string Login,
    [Required, StringLength(100, MinimumLength = 6)] string Password);

public sealed record LoginRequest(
    [Required] string Login,
    [Required] string Password);

public sealed record TokenResponse(string Token, DateTime ExpiresAt, Guid UserId, string Login);
