using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quiz.Api.Dtos;
using Quiz.Api.Services;
using Quiz.Infrastructure.Auth;

namespace Quiz.Api.Controllers;

[ApiController]
[Route("api/quizzes")]
[Authorize(Policy = QuizClaims.HostPolicy)]
public sealed class QuizzesController(QuizService quizzes) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<QuizSummary>> List(CancellationToken ct) =>
        await quizzes.ListAsync(User.GetSubjectId(), ct);

    [HttpGet("{id:guid}")]
    public async Task<QuizResponse> Get(Guid id, CancellationToken ct) =>
        await quizzes.GetAsync(User.GetSubjectId(), id, ct);

    [HttpPost]
    public async Task<ActionResult<QuizResponse>> Create(QuizRequest request, CancellationToken ct)
    {
        var quiz = await quizzes.CreateAsync(User.GetSubjectId(), request, ct);
        return CreatedAtAction(nameof(Get), new { id = quiz.Id }, quiz);
    }

    [HttpPut("{id:guid}")]
    public async Task<QuizResponse> Update(Guid id, QuizRequest request, CancellationToken ct) =>
        await quizzes.UpdateAsync(User.GetSubjectId(), id, request, ct);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await quizzes.DeleteAsync(User.GetSubjectId(), id, ct);
        return NoContent();
    }
}
