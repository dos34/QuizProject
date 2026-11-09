namespace Quiz.Domain.Rules;

public static class Scoring
{
    public const int MaxPoints = 1000;

    /// <summary>
    /// Очки за ответ: неправильный — 0; правильный — от 1000 (мгновенно) до 500 (в последний момент),
    /// линейно по доле использованного времени.
    /// </summary>
    public static int Calculate(bool isCorrect, int elapsedMs, int timeLimitSeconds)
    {
        if (!isCorrect)
        {
            return 0;
        }

        var limitMs = Math.Max(1, timeLimitSeconds * 1000);
        var ratio = Math.Clamp((double)elapsedMs / limitMs, 0, 1);
        return (int)Math.Round(MaxPoints * (1 - ratio / 2), MidpointRounding.AwayFromZero);
    }
}
