using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;

namespace Quiz.Infrastructure.Mongo;

public sealed class MongoHealthCheck(MongoContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await db.Database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("MongoDB is unavailable", ex);
        }
    }
}
