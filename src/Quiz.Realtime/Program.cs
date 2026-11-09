using Quiz.Infrastructure.Auth;
using Quiz.Infrastructure.Messaging;
using Quiz.Infrastructure.Mongo;
using Quiz.Infrastructure.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.AddQuizServiceDefaults("quiz-realtime");

builder.Services.AddQuizMongo(builder.Configuration);
builder.Services.AddQuizJwtAuth(builder.Configuration);
builder.Services.AddQuizMessaging(builder.Configuration, "realtime", x =>
{
    // TODO (Человек 2): консьюмер, который получает события игры из RabbitMQ
    // и рассылает их в браузеры через IHubContext<GameHub, IGameClient>.
});

builder.Services.AddSignalR(o => o.EnableDetailedErrors = builder.Environment.IsDevelopment());

// В docker-compose фронт и хаб за одним nginx (same-origin). CORS нужен только для локальной отладки.
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [])
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

var app = builder.Build();

app.UseQuizServiceDefaults();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// TODO (Человек 2): хаб SignalR. Фронт подключается к /hubs/game и вызывает JoinGame(gameId).
// app.MapHub<GameHub>("/hubs/game");
app.MapQuizHealthChecks();

app.Run();
