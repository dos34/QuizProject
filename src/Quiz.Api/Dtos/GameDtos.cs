using System.ComponentModel.DataAnnotations;
using Quiz.Domain.Entities;

namespace Quiz.Api.Dtos;

public sealed record CreateGameRequest([Required] Guid QuizId);

public sealed record CreateGameResponse(Guid GameId, string Pin);

public sealed record JoinGameRequest(
    [Required, StringLength(6, MinimumLength = 6)] string Pin,
    [Required, StringLength(24, MinimumLength = 1)] string Nickname);

public sealed record JoinGameResponse(Guid GameId, Guid PlayerId, string Nickname, string Token);

public sealed record SubmitAnswerRequest(int QuestionIndex, int OptionIndex);

public sealed record SubmitAnswerResponse(bool Accepted, int ElapsedMs);

public sealed record GameSummary(
    Guid Id,
    string QuizTitle,
    string Pin,
    GameStatus Status,
    int TotalQuestions,
    DateTime CreatedAt,
    DateTime? FinishedAt);

/// <summary>Вопрос глазами клиента. CorrectOptionIndex заполнен только после закрытия вопроса (античит).</summary>
public sealed record QuestionView(
    int Index,
    string Text,
    IReadOnlyList<string> Options,
    int TimeLimitSeconds,
    DateTime? StartedAt,
    DateTime? Deadline,
    int? CorrectOptionIndex,
    string? ImageUrl);

/// <summary>Полное состояние игры — для первого входа и переподключения клиента.</summary>
public sealed record GameStateResponse(
    Guid Id,
    string QuizTitle,
    string Pin,
    GameStatus Status,
    QuestionPhase Phase,
    int CurrentQuestionIndex,
    int TotalQuestions,
    QuestionView? CurrentQuestion,
    int PlayersCount,
    bool? HasAnsweredCurrent,
    DateTime ServerTime);

public sealed record PlayerView(Guid Id, string Nickname, int Score);
