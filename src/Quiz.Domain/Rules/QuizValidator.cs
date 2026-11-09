using Quiz.Domain.Entities;

namespace Quiz.Domain.Rules;

public static class QuizValidator
{
    public const int MinOptions = 2;
    public const int MaxOptions = 6;
    public const int MinTimeLimit = 5;
    public const int MaxTimeLimit = 300;
    public const int MaxQuestions = 100;

    public static IReadOnlyList<string> Validate(string title, IReadOnlyList<Question> questions)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(title))
        {
            errors.Add("Название викторины не может быть пустым.");
        }
        else if (title.Length > 200)
        {
            errors.Add("Название викторины длиннее 200 символов.");
        }

        if (questions.Count == 0)
        {
            errors.Add("В викторине должен быть хотя бы один вопрос.");
        }
        else if (questions.Count > MaxQuestions)
        {
            errors.Add($"В викторине не может быть больше {MaxQuestions} вопросов.");
        }

        for (var i = 0; i < questions.Count; i++)
        {
            var q = questions[i];
            var n = i + 1;

            if (string.IsNullOrWhiteSpace(q.Text))
            {
                errors.Add($"Вопрос {n}: пустой текст.");
            }

            if (q.Options.Count is < MinOptions or > MaxOptions)
            {
                errors.Add($"Вопрос {n}: вариантов должно быть от {MinOptions} до {MaxOptions}.");
            }

            if (q.Options.Any(string.IsNullOrWhiteSpace))
            {
                errors.Add($"Вопрос {n}: есть пустой вариант ответа.");
            }

            if (q.CorrectOptionIndex < 0 || q.CorrectOptionIndex >= q.Options.Count)
            {
                errors.Add($"Вопрос {n}: индекс правильного ответа вне диапазона.");
            }

            if (q.TimeLimitSeconds is < MinTimeLimit or > MaxTimeLimit)
            {
                errors.Add($"Вопрос {n}: лимит времени должен быть от {MinTimeLimit} до {MaxTimeLimit} секунд.");
            }
        }

        return errors;
    }
}
