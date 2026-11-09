using Quiz.Domain.Entities;
using Quiz.Domain.Rules;

namespace Quiz.UnitTests;

public class LeaderboardTests
{
    private static readonly DateTime T0 = new(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);

    private static Player P(string name, int score, int joinedSecond) =>
        new() { Nickname = name, NicknameKey = name, Score = score, JoinedAt = T0.AddSeconds(joinedSecond) };

    [Fact]
    public void SortedByScoreDescending()
    {
        var board = Leaderboard.Build([P("a", 100, 0), P("b", 300, 1), P("c", 200, 2)]);
        Assert.Equal(["b", "c", "a"], board.Select(e => e.Nickname));
        Assert.Equal([1, 2, 3], board.Select(e => e.Rank));
    }

    [Fact]
    public void EqualScores_ShareRank_AndKeepJoinOrder()
    {
        var board = Leaderboard.Build([P("late", 500, 5), P("early", 500, 1), P("low", 10, 0)]);
        Assert.Equal(["early", "late", "low"], board.Select(e => e.Nickname));
        Assert.Equal([1, 1, 3], board.Select(e => e.Rank));
    }

    [Fact]
    public void Empty_ReturnsEmpty() => Assert.Empty(Leaderboard.Build([]));
}
