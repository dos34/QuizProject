using Quiz.Infrastructure.Messaging;
using Quiz.Infrastructure.Mongo;
using Quiz.Infrastructure.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.AddQuizServiceDefaults("quiz-worker");

builder.Services.AddQuizMongo(builder.Configuration);
builder.Services.AddQuizMessaging(builder.Configuration, "worker", x =>
{
    // TODO (Человек 2): зарегистрировать консьюмеры движка игры:
    //   GameStarted   -> открыть первый вопрос, опубликовать QuestionStarted, запланировать CloseQuestion на дедлайн
    //   CloseQuestion -> посчитать очки (Quiz.Domain.Rules.Scoring), фаза Closed, опубликовать QuestionClosed + LeaderboardUpdated
    //   AdvanceGame   -> следующий вопрос или завершение игры (GameFinished)
    //   AnswerSubmitted -> закрыть вопрос досрочно, если ответили все
    // Пример: x.AddConsumer<GameStartedConsumer>();
});

builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();

app.UseQuizServiceDefaults();
app.MapQuizHealthChecks();
app.MapGet("/", () => "Quiz worker is running");

app.Run();
