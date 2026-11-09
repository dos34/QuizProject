namespace Quiz.Domain.Entities;

public sealed class QuizEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OwnerId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public List<Question> Questions { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Вопрос с одиночным выбором. Хранится вложенным массивом внутри викторины.</summary>
public sealed class Question
{
    public required string Text { get; set; }
    public List<string> Options { get; set; } = [];
    public int CorrectOptionIndex { get; set; }
    public int TimeLimitSeconds { get; set; } = 20;
    public string? ImageUrl { get; set; }
}
