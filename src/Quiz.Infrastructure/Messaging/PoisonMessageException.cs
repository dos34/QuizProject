namespace Quiz.Infrastructure.Messaging;

/// <summary>
/// Сообщение невозможно обработать никогда (например, CloseQuestion для несуществующей игры).
/// Ретраи для него не выполняются — оно сразу уходит в очередь ошибок (*_error, DLQ).
/// </summary>
public sealed class PoisonMessageException(string message) : Exception(message);
