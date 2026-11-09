namespace Quiz.Realtime.Hubs;

public static class GameGroups
{
    /// <summary>Все участники игры (ведущий + игроки).</summary>
    public static string All(Guid gameId) => $"game:{gameId}";

    /// <summary>Только ведущий игры.</summary>
    public static string Host(Guid gameId) => $"game:{gameId}:host";
}
