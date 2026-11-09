namespace Quiz.Contracts.Events;

// События — факты, которые уже произошли. Публикуются в RabbitMQ (fanout exchange на каждый тип),
// подписчики: Worker (движок игры) и Realtime (SignalR).
// Правило античита: правильный ответ НЕ передаётся до QuestionClosed.

public sealed record PlayerJoined(Guid GameId, Guid PlayerId, string Nickname, int PlayersCount);

public sealed record GameStarted(Guid GameId, DateTime StartedAt);

public sealed record QuestionStarted(
    Guid GameId,
    int QuestionIndex,
    int TotalQuestions,
    string Text,
    IReadOnlyList<string> Options,
    int TimeLimitSeconds,
    DateTime StartedAt,
    DateTime Deadline);

public sealed record AnswerSubmitted(
    Guid GameId,
    int QuestionIndex,
    Guid PlayerId,
    int AnsweredCount,
    int PlayersCount);

public sealed record QuestionClosed(
    Guid GameId,
    int QuestionIndex,
    int CorrectOptionIndex,
    IReadOnlyList<int> OptionCounts,
    bool IsLastQuestion);

public sealed record LeaderboardUpdated(Guid GameId, int QuestionIndex, IReadOnlyList<LeaderboardEntry> Entries);

public sealed record GameFinished(Guid GameId, DateTime FinishedAt, IReadOnlyList<LeaderboardEntry> Leaderboard);
