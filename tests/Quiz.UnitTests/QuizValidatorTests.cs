using Quiz.Domain.Entities;
using Quiz.Domain.Rules;

namespace Quiz.UnitTests;

public class QuizValidatorTests
{
    private static Question Valid() => new()
    {
        Text = "Столица Франции?",
        Options = ["Лион", "Париж", "Марсель"],
        CorrectOptionIndex = 1,
        TimeLimitSeconds = 20,
    };

    [Fact]
    public void ValidQuiz_HasNoErrors() => Assert.Empty(QuizValidator.Validate("География", [Valid()]));

    [Fact]
    public void EmptyTitle_IsError() => Assert.NotEmpty(QuizValidator.Validate("  ", [Valid()]));

    [Fact]
    public void NoQuestions_IsError() => Assert.NotEmpty(QuizValidator.Validate("t", []));

    [Fact]
    public void SingleOption_IsError()
    {
        var q = Valid();
        q.Options = ["one"];
        q.CorrectOptionIndex = 0;
        Assert.NotEmpty(QuizValidator.Validate("t", [q]));
    }

    [Fact]
    public void CorrectIndexOutOfRange_IsError()
    {
        var q = Valid();
        q.CorrectOptionIndex = 3;
        Assert.NotEmpty(QuizValidator.Validate("t", [q]));
    }

    [Fact]
    public void EmptyOption_IsError()
    {
        var q = Valid();
        q.Options[2] = " ";
        Assert.NotEmpty(QuizValidator.Validate("t", [q]));
    }

    [Theory]
    [InlineData(4)]
    [InlineData(301)]
    public void TimeLimitOutOfRange_IsError(int seconds)
    {
        var q = Valid();
        q.TimeLimitSeconds = seconds;
        Assert.NotEmpty(QuizValidator.Validate("t", [q]));
    }

    [Fact]
    public void ErrorMessage_ContainsQuestionNumber()
    {
        var bad = Valid();
        bad.Text = "";
        var errors = QuizValidator.Validate("t", [Valid(), bad]);
        Assert.Contains(errors, e => e.StartsWith("Вопрос 2", StringComparison.Ordinal));
    }
}
