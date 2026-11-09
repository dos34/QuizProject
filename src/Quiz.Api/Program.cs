using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Quiz.Api.Errors;
using Quiz.Api.Services;
using Quiz.Domain.Entities;
using Quiz.Infrastructure.Auth;
using Quiz.Infrastructure.Messaging;
using Quiz.Infrastructure.Mongo;
using Quiz.Infrastructure.Observability;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddQuizServiceDefaults("quiz-api");

builder.Services.AddQuizMongo(builder.Configuration);
builder.Services.AddQuizMessaging(builder.Configuration, "api"); // API только публикует, consumer'ов нет
builder.Services.AddQuizJwtAuth(builder.Configuration);

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<QuizService>();
builder.Services.AddScoped<GameService>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseQuizServiceDefaults();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapQuizHealthChecks();
app.MapOpenApi();                 // /openapi/v1.json
app.MapScalarApiReference("/docs"); // интерактивная документация

app.Run();

public partial class Program;
