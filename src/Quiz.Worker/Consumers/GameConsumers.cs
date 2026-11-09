using MassTransit;
using Quiz.Contracts.Commands;
using Quiz.Contracts.Events;
using Quiz.Worker.Engine;

namespace Quiz.Worker.Consumers;

/// <summary>Ведущий нажал «Старт» → показать первый вопрос.</summary>
public sealed class GameStartedConsumer(GameEngine engine) : IConsumer<GameStarted>
{
    public Task Consume(ConsumeContext<GameStarted> context) =>
        engine.StartGameAsync(context.Message.GameId, context.CancellationToken);
}

/// <summary>Сработал таймер вопроса (отложенное сообщение) или ведущий закрыл вопрос досрочно.</summary>
public sealed class CloseQuestionConsumer(GameEngine engine) : IConsumer<CloseQuestion>
{
    public Task Consume(ConsumeContext<CloseQuestion> context) =>
        engine.CloseQuestionAsync(context.Message.GameId, context.Message.QuestionIndex, context.CancellationToken);
}

/// <summary>Ведущий нажал «Далее».</summary>
public sealed class AdvanceGameConsumer(GameEngine engine) : IConsumer<AdvanceGame>
{
    public Task Consume(ConsumeContext<AdvanceGame> context) =>
        engine.AdvanceAsync(context.Message.GameId, context.Message.ExpectedQuestionIndex, context.CancellationToken);
}

/// <summary>Если ответили все игроки — не ждём таймер и закрываем вопрос сразу.</summary>
public sealed class AnswerSubmittedConsumer(GameEngine engine) : IConsumer<AnswerSubmitted>
{
    public Task Consume(ConsumeContext<AnswerSubmitted> context)
    {
        var m = context.Message;
        return m.AnsweredCount >= m.PlayersCount
            ? engine.CloseQuestionAsync(m.GameId, m.QuestionIndex, context.CancellationToken)
            : Task.CompletedTask;
    }
}
