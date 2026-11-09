namespace Quiz.Domain.Entities;

public enum GameStatus
{
    Lobby,
    Running,
    Finished,
    Abandoned,
}

/// <summary>Фаза текущего вопроса внутри запущенной игры.</summary>
public enum QuestionPhase
{
    /// <summary>Ни один вопрос ещё не показан.</summary>
    None,
    /// <summary>Вопрос показан, ответы принимаются до Deadline.</summary>
    Open,
    /// <summary>Вопрос закрыт, очки посчитаны, ждём «Далее» от ведущего.</summary>
    Closed,
}

public sealed class GameSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuizId { get; set; }
    public Guid HostId { get; set; }
    public required string QuizTitle { get; set; }
    public required string Pin { get; set; }

    /// <summary>Снимок вопросов на момент создания игры: правка викторины не ломает идущую игру.</summary>
    public List<Question> Questions { get; set; } = [];

    public GameStatus Status { get; set; } = GameStatus.Lobby;

    /// <summary>Lobby/Running. Нужен для partial unique index по PIN среди активных игр.</summary>
    public bool IsActive { get; set; } = true;

    public int CurrentQuestionIndex { get; set; } = -1;
    public QuestionPhase Phase { get; set; } = QuestionPhase.None;
    public DateTime? QuestionStartedAt { get; set; }
    public DateTime? QuestionDeadline { get; set; }

    // State management для Worker
    public string? LeaseOwner { get; set; }
    public DateTime? LeaseUntil { get; set; }
    public int Attempts { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public Question? CurrentQuestion =>
        CurrentQuestionIndex >= 0 && CurrentQuestionIndex < Questions.Count ? Questions[CurrentQuestionIndex] : null;

    public bool IsLastQuestion => CurrentQuestionIndex == Questions.Count - 1;
}
