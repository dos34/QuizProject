namespace Quiz.Contracts;

public sealed record LeaderboardEntry(int Rank, Guid PlayerId, string Nickname, int Score);
