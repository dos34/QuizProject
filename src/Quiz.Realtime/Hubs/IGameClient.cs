using Quiz.Contracts.Events;

namespace Quiz.Realtime.Hubs;

/// <summary>
/// Контракт с фронтом (web/common.js, play.html, host.html): методы, которые сервер вызывает у клиента.
/// Имена методов = имена событий на фронте — не переименовывать.
/// Группы: game:{gameId} — все участники игры, game:{gameId}:host — только ведущий (AnswerProgress).
/// Хаб: /hubs/game, метод JoinGame(Guid gameId); токен приходит в query-параметре access_token.
/// </summary>
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
