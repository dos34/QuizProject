using Quiz.Contracts.Events;

namespace Quiz.Realtime.Hubs;

/// <summary>Методы, которые сервер вызывает у клиента. Имена методов = имена событий на фронте.</summary>
public interface IGameClient
{
    Task PlayerJoined(PlayerJoined message);
    Task GameStarted(GameStarted message);
    Task QuestionStarted(QuestionStarted message);
    Task AnswerProgress(AnswerProgress message);
    Task QuestionClosed(QuestionClosed message);
    Task LeaderboardUpdated(LeaderboardUpdated message);
    Task GameFinished(GameFinished message);
}

/// <summary>Только для ведущего: сколько игроков уже ответило.</summary>
public sealed record AnswerProgress(Guid GameId, int QuestionIndex, int AnsweredCount, int PlayersCount);
