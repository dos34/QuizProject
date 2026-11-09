namespace Quiz.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string Section = "Jwt";

    /// <summary>Симметричный ключ подписи. Только из env/секретов, не из репозитория.</summary>
    public string Key { get; set; } = "";
    public string Issuer { get; set; } = "quiz-api";
    public string Audience { get; set; } = "quiz";
    public TimeSpan HostTokenLifetime { get; set; } = TimeSpan.FromHours(12);
    public TimeSpan PlayerTokenLifetime { get; set; } = TimeSpan.FromHours(4);
}
