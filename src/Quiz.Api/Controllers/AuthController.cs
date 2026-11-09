using Microsoft.AspNetCore.Mvc;
using Quiz.Api.Dtos;
using Quiz.Api.Services;

namespace Quiz.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AuthService auth) : ControllerBase
{
    /// <summary>Регистрация ведущего.</summary>
    [HttpPost("register")]
    public async Task<ActionResult<TokenResponse>> Register(RegisterRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await auth.RegisterAsync(request, ct));

    /// <summary>Вход ведущего, выдаёт JWT.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<TokenResponse>> Login(LoginRequest request, CancellationToken ct) =>
        Ok(await auth.LoginAsync(request, ct));
}
