namespace Quiz.Domain.Entities;

public sealed class Answer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GameId { get; set; }
    public int QuestionIndex { get; set; }
    public Guid PlayerId { get; set; }
    public int OptionIndex { get; set; }

    /// <summary>Серверное время приёма ответа. Клиентскому времени не доверяем.</summary>
    public DateTime SubmittedAt { get; set; }

    /// <summary>Сколько миллисекунд прошло от показа вопроса до ответа.</summary>
    public int ElapsedMs { get; set; }

    // Заполняются воркером при закрытии вопроса
    public bool? IsCorrect { get; set; }
    public int Points { get; set; }
}
