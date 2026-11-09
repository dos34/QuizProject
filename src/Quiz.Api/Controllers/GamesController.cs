using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quiz.Api.Dtos;
using Quiz.Api.Services;
using Quiz.Contracts;
using Quiz.Infrastructure.Auth;

namespace Quiz.Api.Controllers;

[ApiController]
[Route("api/games")]
[Authorize]
public sealed class GamesController(GameService games) : ControllerBase
{
    /// <summary>Создать игровую сессию по викторине и получить PIN.</summary>
    [HttpPost]
    [Authorize(Policy = QuizClaims.HostPolicy)]
    public async Task<ActionResult<CreateGameResponse>> Create(CreateGameRequest request, CancellationToken ct)
    {
        var result = await games.CreateAsync(User.GetSubjectId(), request.QuizId, ct);
        return CreatedAtAction(nameof(Get), new { id = result.GameId }, result);
    }

    /// <summary>История игр ведущего.</summary>
    [HttpGet]
    [Authorize(Policy = QuizClaims.HostPolicy)]
    public async Task<IReadOnlyList<GameSummary>> List(CancellationToken ct) =>
        await games.ListForHostAsync(User.GetSubjectId(), ct);

    /// <summary>Текущее состояние игры (для ведущего и игроков; используется при переподключении).</summary>
    [HttpGet("{id:guid}")]
    public async Task<GameStateResponse> Get(Guid id, CancellationToken ct) =>
        await games.GetStateAsync(id, User, ct);

    [HttpPost("{id:guid}/start")]
    [Authorize(Policy = QuizClaims.HostPolicy)]
    public async Task<IActionResult> Start(Guid id, CancellationToken ct)
    {
        await games.StartAsync(User.GetSubjectId(), id, ct);
        return Accepted();
    }

    /// <summary>Следующий вопрос (или досрочно закрыть текущий, если он ещё открыт).</summary>
    [HttpPost("{id:guid}/next")]
    [Authorize(Policy = QuizClaims.HostPolicy)]
    public async Task<IActionResult> Next(Guid id, CancellationToken ct)
    {
        await games.NextAsync(User.GetSubjectId(), id, ct);
        return Accepted();
    }

    /// <summary>Вход игрока по PIN и никнейму. Возвращает токен сессии игрока.</summary>
    [HttpPost("join")]
    [AllowAnonymous]
    public async Task<JoinGameResponse> Join(JoinGameRequest request, CancellationToken ct) =>
        await games.JoinAsync(request, ct);

    [HttpPost("{id:guid}/answers")]
    [Authorize(Policy = QuizClaims.PlayerPolicy)]
    public async Task<ActionResult<SubmitAnswerResponse>> Answer(Guid id, SubmitAnswerRequest request, CancellationToken ct) =>
        Accepted(await games.SubmitAnswerAsync(id, User, request, ct));

    [HttpGet("{id:guid}/leaderboard")]
    public async Task<IReadOnlyList<LeaderboardEntry>> Leaderboard(Guid id, CancellationToken ct) =>
        await games.GetLeaderboardAsync(id, User, ct);
}
