using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace SmartSolarMicrogrid.API.Data;

public class MongoHealthCheck : IHealthCheck
{
    private readonly IMongoDatabase _database;

    public MongoHealthCheck(IMongoDatabase database)
    {
        _database = database;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await _database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy("MongoDB is connected");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("MongoDB is unreachable", ex);
        }
    }
}
