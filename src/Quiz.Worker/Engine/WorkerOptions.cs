namespace Quiz.Worker.Engine;

public sealed class WorkerOptions
{
    public const string Section = "Worker";

    /// <summary>На сколько воркер захватывает игру на время обработки одного шага.</summary>
    public TimeSpan LeaseTtl { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>Запас после дедлайна: ответы «в последнюю миллисекунду» успеют записаться до подсчёта.</summary>
    public TimeSpan CloseGrace { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>Как часто искать зависшие и брошенные игры.</summary>
    public TimeSpan MaintenanceInterval { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>Вопрос считается зависшим, если он открыт дольше дедлайна на это время.</summary>
    public TimeSpan StuckAfter { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>Игра без изменений дольше этого времени считается брошенной.</summary>
    public TimeSpan AbandonAfter { get; set; } = TimeSpan.FromHours(1);
}
