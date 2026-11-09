using Quiz.Contracts;
using Quiz.Domain.Entities;

namespace Quiz.Domain.Rules;

public static class Leaderboard
{
    /// <summary>Сортировка по очкам; при равенстве — кто раньше зашёл. Одинаковые очки — одинаковое место.</summary>
    public static IReadOnlyList<LeaderboardEntry> Build(IEnumerable<Player> players)
    {
        var ordered = players
            .OrderByDescending(p => p.Score)
            .ThenBy(p => p.JoinedAt)
            .ToList();

        var result = new List<LeaderboardEntry>(ordered.Count);
        for (var i = 0; i < ordered.Count; i++)
        {
            var rank = i > 0 && ordered[i].Score == ordered[i - 1].Score ? result[i - 1].Rank : i + 1;
            result.Add(new LeaderboardEntry(rank, ordered[i].Id, ordered[i].Nickname, ordered[i].Score));
        }

        return result;
    }
}
