using Quiz.Domain.Entities;
using Quiz.Domain.Rules;

namespace Quiz.UnitTests;

public class AnswerRulesTests
{
    private static readonly DateTime Start = new(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);

    private static GameSession OpenGame() => new()
    {
        QuizTitle = "t",
        Pin = "123456",
        Status = GameStatus.Running,
        Phase = QuestionPhase.Open,
        CurrentQuestionIndex = 0,
        QuestionStartedAt = Start,
        QuestionDeadline = Start.AddSeconds(20),
        Questions = [new Question { Text = "2+2?", Options = ["3", "4"], CorrectOptionIndex = 1 }],
    };

    [Fact]
    public void ValidAnswer_IsAccepted() =>
        Assert.Equal(AnswerRejection.None, AnswerRules.Check(OpenGame(), 0, 1, Start.AddSeconds(5)));

    [Fact]
    public void AnswerExactlyAtDeadline_IsAccepted() =>
        Assert.Equal(AnswerRejection.None, AnswerRules.Check(OpenGame(), 0, 0, Start.AddSeconds(20)));

    [Fact]
    public void AnswerAfterDeadline_IsRejected() =>
        Assert.Equal(AnswerRejection.TimeIsUp, AnswerRules.Check(OpenGame(), 0, 0, Start.AddSeconds(20.001)));

    [Fact]
    public void AnswerToAnotherQuestion_IsRejected() =>
        Assert.Equal(AnswerRejection.WrongQuestion, AnswerRules.Check(OpenGame(), 1, 0, Start));

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    public void InvalidOption_IsRejected(int option) =>
        Assert.Equal(AnswerRejection.InvalidOption, AnswerRules.Check(OpenGame(), 0, option, Start));

    [Fact]
    public void ClosedQuestion_IsRejected()
    {
        var game = OpenGame();
        game.Phase = QuestionPhase.Closed;
        Assert.Equal(AnswerRejection.QuestionNotOpen, AnswerRules.Check(game, 0, 0, Start));
    }

    [Theory]
    [InlineData(GameStatus.Lobby)]
    [InlineData(GameStatus.Finished)]
    [InlineData(GameStatus.Abandoned)]
    public void NotRunningGame_IsRejected(GameStatus status)
    {
        var game = OpenGame();
        game.Status = status;
        Assert.Equal(AnswerRejection.GameNotRunning, AnswerRules.Check(game, 0, 0, Start));
    }
}
