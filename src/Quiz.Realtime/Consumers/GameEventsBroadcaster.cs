using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Quiz.Contracts.Events;
using Quiz.Realtime.Hubs;

namespace Quiz.Realtime.Consumers;

/// <summary>
/// Слушает события игры из RabbitMQ и рассылает их клиентам нужной игры через SignalR.
/// Одна очередь (realtime-game-events-broadcaster), привязанная ко всем exchange событий.
/// </summary>
public sealed class GameEventsBroadcaster(IHubContext<GameHub, IGameClient> hub) :
    IConsumer<PlayerJoined>,
    IConsumer<GameStarted>,
    IConsumer<QuestionStarted>,
    IConsumer<AnswerSubmitted>,
    IConsumer<QuestionClosed>,
    IConsumer<LeaderboardUpdated>,
    IConsumer<GameFinished>
{
    public Task Consume(ConsumeContext<PlayerJoined> context) =>
        hub.Clients.Group(GameGroups.All(context.Message.GameId)).PlayerJoined(context.Message);

    public Task Consume(ConsumeContext<GameStarted> context) =>
        hub.Clients.Group(GameGroups.All(context.Message.GameId)).GameStarted(context.Message);

    public Task Consume(ConsumeContext<QuestionStarted> context) =>
        hub.Clients.Group(GameGroups.All(context.Message.GameId)).QuestionStarted(context.Message);

    // Прогресс ответов видит только ведущий; кто именно что ответил — не раскрываем.
    public Task Consume(ConsumeContext<AnswerSubmitted> context)
    {
        var m = context.Message;
        return hub.Clients.Group(GameGroups.Host(m.GameId))
            .AnswerProgress(new AnswerProgress(m.GameId, m.QuestionIndex, m.AnsweredCount, m.PlayersCount));
    }

    public Task Consume(ConsumeContext<QuestionClosed> context) =>
        hub.Clients.Group(GameGroups.All(context.Message.GameId)).QuestionClosed(context.Message);

    public Task Consume(ConsumeContext<LeaderboardUpdated> context) =>
        hub.Clients.Group(GameGroups.All(context.Message.GameId)).LeaderboardUpdated(context.Message);

    public Task Consume(ConsumeContext<GameFinished> context) =>
        hub.Clients.Group(GameGroups.All(context.Message.GameId)).GameFinished(context.Message);
}
