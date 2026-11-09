namespace Quiz.Contracts.Commands;

// Команды — просьба что-то сделать. Обрабатывает только Worker.

/// <summary>Закрыть вопрос. Планируется отложенным сообщением на момент дедлайна.</summary>
public sealed record CloseQuestion(Guid GameId, int QuestionIndex);

/// <summary>
/// Перейти к следующему вопросу (или завершить игру после последнего).
/// ExpectedQuestionIndex защищает от двойного нажатия «Далее»: обработается только первая команда.
/// </summary>
public sealed record AdvanceGame(Guid GameId, int ExpectedQuestionIndex);
