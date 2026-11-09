using Quiz.Domain.Rules;

namespace Quiz.UnitTests;

public class PinGeneratorTests
{
    [Fact]
    public void Generate_ReturnsSixDigits()
    {
        for (var i = 0; i < 1000; i++)
        {
            Assert.True(PinGenerator.IsValid(PinGenerator.Generate()));
        }
    }

    [Theory]
    [InlineData("123456", true)]
    [InlineData("000001", true)]
    [InlineData("12345", false)]
    [InlineData("1234567", false)]
    [InlineData("12a456", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValid_ChecksFormat(string? pin, bool expected) => Assert.Equal(expected, PinGenerator.IsValid(pin));
}
