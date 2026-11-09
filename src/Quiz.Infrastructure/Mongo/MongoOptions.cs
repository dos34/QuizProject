namespace Quiz.Infrastructure.Mongo;

public sealed class MongoOptions
{
    public const string Section = "Mongo";

    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string Database { get; set; } = "quiz";
}
