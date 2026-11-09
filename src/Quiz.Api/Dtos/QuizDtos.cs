using System.ComponentModel.DataAnnotations;
using Quiz.Domain.Entities;

namespace Quiz.Api.Dtos;

public sealed record QuestionDto(
    [Required, StringLength(500)] string Text,
    [Required] List<string> Options,
    int CorrectOptionIndex,
    int TimeLimitSeconds = 20,
    [StringLength(1000)] string? ImageUrl = null)
{
    public Question ToEntity() => new()
    {
        Text = Text.Trim(),
        Options = Options.Select(o => o?.Trim() ?? "").ToList(),
        CorrectOptionIndex = CorrectOptionIndex,
        TimeLimitSeconds = TimeLimitSeconds,
        ImageUrl = ImageUrl,
    };

    public static QuestionDto From(Question q) =>
        new(q.Text, q.Options, q.CorrectOptionIndex, q.TimeLimitSeconds, q.ImageUrl);
}

public sealed record QuizRequest(
    [Required, StringLength(200)] string Title,
    [StringLength(2000)] string? Description,
    [Required] List<QuestionDto> Questions);

public sealed record QuizResponse(
    Guid Id,
    string Title,
    string? Description,
    IReadOnlyList<QuestionDto> Questions,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static QuizResponse From(QuizEntity q) =>
        new(q.Id, q.Title, q.Description, q.Questions.Select(QuestionDto.From).ToList(), q.CreatedAt, q.UpdatedAt);
}

public sealed record QuizSummary(Guid Id, string Title, int QuestionsCount, DateTime UpdatedAt);
