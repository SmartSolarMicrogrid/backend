using MongoDB.Driver;

namespace SmartSolarMicrogrid.API.Data;

public interface ITransactionRunner
{
    Task RunAsync(Func<IClientSessionHandle, CancellationToken, Task> work, CancellationToken ct = default);
}

public class MongoTransactionRunner : ITransactionRunner
{
    private readonly IMongoClient _client;

    public MongoTransactionRunner(IMongoClient client)
    {
        _client = client;
    }

    public async Task RunAsync(Func<IClientSessionHandle, CancellationToken, Task> work, CancellationToken ct = default)
    {
        using var session = await _client.StartSessionAsync(cancellationToken: ct);
        await session.WithTransactionAsync(async (s, token) =>
        {
            await work(s, token);
            return true;
        }, cancellationToken: ct);
    }
}
