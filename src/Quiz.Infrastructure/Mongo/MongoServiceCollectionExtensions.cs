using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Quiz.Infrastructure.Mongo;

public static class MongoServiceCollectionExtensions
{
    public static IServiceCollection AddQuizMongo(this IServiceCollection services, IConfiguration configuration)
    {
        MongoMappings.Register();

        services.AddOptions<MongoOptions>().Bind(configuration.GetSection(MongoOptions.Section));
        services.AddSingleton<IMongoClient>(sp =>
            new MongoClient(sp.GetRequiredService<IOptions<MongoOptions>>().Value.ConnectionString));
        services.AddSingleton<MongoContext>();
        services.AddSingleton<GameEventLog>();
        services.AddHostedService<MongoIndexInitializer>();
        services.AddHealthChecks().AddCheck<MongoHealthCheck>("mongo", tags: ["ready"]);

        return services;
    }
}
