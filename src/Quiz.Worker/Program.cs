using Quiz.Infrastructure.Messaging;
using Quiz.Infrastructure.Mongo;
using Quiz.Infrastructure.Observability;
using Quiz.Worker.Consumers;
using Quiz.Worker.Engine;

var builder = WebApplication.CreateBuilder(args);

builder.AddQuizServiceDefaults("quiz-worker");

builder.Services.AddQuizMongo(builder.Configuration);
builder.Services.AddQuizMessaging(builder.Configuration, "worker", x =>
{
    x.AddConsumer<GameStartedConsumer>();
    x.AddConsumer<CloseQuestionConsumer>();
    x.AddConsumer<AdvanceGameConsumer>();
    x.AddConsumer<AnswerSubmittedConsumer>();
});

builder.Services.AddOptions<WorkerOptions>().Bind(builder.Configuration.GetSection(WorkerOptions.Section));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<GameLease>();
builder.Services.AddScoped<GameEngine>();
builder.Services.AddHostedService<MaintenanceService>();

var app = builder.Build();

app.UseQuizServiceDefaults();
app.MapQuizHealthChecks();
app.MapGet("/", () => "Quiz worker is running");

app.Run();
