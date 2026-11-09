using Quiz.Infrastructure.Auth;
using Quiz.Infrastructure.Messaging;
using Quiz.Infrastructure.Mongo;
using Quiz.Infrastructure.Observability;
using Quiz.Realtime.Consumers;
using Quiz.Realtime.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.AddQuizServiceDefaults("quiz-realtime");

builder.Services.AddQuizMongo(builder.Configuration);
builder.Services.AddQuizJwtAuth(builder.Configuration);
builder.Services.AddQuizMessaging(builder.Configuration, "realtime", x =>
{
    x.AddConsumer<GameEventsBroadcaster>();
});

// Одна реплика без backplane. При масштабировании: AddStackExchangeRedis(...) как backplane.
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

app.MapHub<GameHub>("/hubs/game");
app.MapQuizHealthChecks();

app.Run();
