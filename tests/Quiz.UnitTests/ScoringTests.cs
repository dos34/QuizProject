using Quiz.Domain.Rules;

namespace Quiz.UnitTests;

public class ScoringTests
{
    [Fact]
    public void WrongAnswer_GivesZero() => Assert.Equal(0, Scoring.Calculate(false, 100, 20));

    [Fact]
    public void InstantCorrectAnswer_GivesMax() => Assert.Equal(1000, Scoring.Calculate(true, 0, 20));

    [Fact]
    public void LastMomentCorrectAnswer_GivesHalf() => Assert.Equal(500, Scoring.Calculate(true, 20_000, 20));

    [Fact]
    public void HalfTime_GivesThreeQuarters() => Assert.Equal(750, Scoring.Calculate(true, 10_000, 20));

    [Theory]
    [InlineData(-500)]
    [InlineData(999_999)]
    public void ElapsedOutOfRange_IsClamped(int elapsed)
    {
        var points = Scoring.Calculate(true, elapsed, 20);
        Assert.InRange(points, 500, 1000);
    }

    [Fact]
    public void FasterAnswer_GivesMorePoints() =>
        Assert.True(Scoring.Calculate(true, 1_000, 20) > Scoring.Calculate(true, 5_000, 20));
}
