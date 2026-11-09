namespace Quiz.Domain.Entities;

public sealed class Player
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GameId { get; set; }
    public required string Nickname { get; set; }

    /// <summary>Никнейм в нижнем регистре — для уникального индекса внутри игры.</summary>
    public required string NicknameKey { get; set; }

    public int Score { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
