using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Quiz.Domain.Entities;

namespace Quiz.Infrastructure.Mongo;

public sealed class MongoContext
{
    public MongoContext(IMongoClient client, IOptions<MongoOptions> options)
    {
        Database = client.GetDatabase(options.Value.Database);
        Users = Database.GetCollection<User>("users");
        Quizzes = Database.GetCollection<QuizEntity>("quizzes");
        Games = Database.GetCollection<GameSession>("games");
        Players = Database.GetCollection<Player>("players");
        Answers = Database.GetCollection<Answer>("answers");
        Events = Database.GetCollection<GameEvent>("game_events");
    }

    public IMongoDatabase Database { get; }
    public IMongoCollection<User> Users { get; }
    public IMongoCollection<QuizEntity> Quizzes { get; }
    public IMongoCollection<GameSession> Games { get; }
    public IMongoCollection<Player> Players { get; }
    public IMongoCollection<Answer> Answers { get; }
    public IMongoCollection<GameEvent> Events { get; }
}
