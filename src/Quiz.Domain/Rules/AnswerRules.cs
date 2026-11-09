using Quiz.Domain.Entities;

namespace Quiz.Domain.Rules;

public enum AnswerRejection
{
    None,
    GameNotRunning,
    QuestionNotOpen,
    WrongQuestion,
    TimeIsUp,
    InvalidOption,
}

public static class AnswerRules
{
    /// <summary>Проверка, можно ли принять ответ прямо сейчас (по серверному времени).</summary>
    public static AnswerRejection Check(GameSession game, int questionIndex, int optionIndex, DateTime now)
    {
        if (game.Status != GameStatus.Running)
        {
            return AnswerRejection.GameNotRunning;
        }

        if (game.Phase != QuestionPhase.Open || game.CurrentQuestion is null)
        {
            return AnswerRejection.QuestionNotOpen;
        }

        if (game.CurrentQuestionIndex != questionIndex)
        {
            return AnswerRejection.WrongQuestion;
        }

        if (game.QuestionDeadline is null || now > game.QuestionDeadline.Value)
        {
            return AnswerRejection.TimeIsUp;
        }

        if (optionIndex < 0 || optionIndex >= game.CurrentQuestion.Options.Count)
        {
            return AnswerRejection.InvalidOption;
        }

        return AnswerRejection.None;
    }
}
