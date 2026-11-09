using System.Security.Cryptography;

namespace Quiz.Domain.Rules;

public static class PinGenerator
{
    public const int Length = 6;

    public static string Generate() =>
        RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", System.Globalization.CultureInfo.InvariantCulture);

    public static bool IsValid(string? pin) => pin is { Length: Length } && pin.All(char.IsAsciiDigit);
}
