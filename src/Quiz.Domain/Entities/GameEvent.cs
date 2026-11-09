namespace Quiz.Domain.Entities;

/// <summary>Журнал событий игры — для аудита и отладки.</summary>
public sealed class GameEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GameId { get; set; }
    public required string Type { get; set; }
    public required string Source { get; set; }
    public string? Payload { get; set; }
    public DateTime At { get; set; } = DateTime.UtcNow;
}
