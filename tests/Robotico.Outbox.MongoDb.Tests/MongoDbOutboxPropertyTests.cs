using CsCheck;
using MongoDB.Driver;
using Xunit;

namespace Robotico.Outbox.MongoDb.Tests;

/// <summary>
/// Property-style checks for <see cref="MongoDbOutbox"/> when no client session is used (commit is a no-op success).
/// </summary>
public sealed class MongoDbOutboxPropertyTests
{
    private static IMongoDatabase GetDatabase()
    {
        string connectionString = Environment.GetEnvironmentVariable("ROBOTICO_MONGO_CONNECTION") ?? "mongodb://127.0.0.1:27017";
        MongoClient client = new(connectionString);
        return client.GetDatabase("RoboticoOutboxTests");
    }

    [Fact]
    public void CommitAsync_without_session_remains_success_across_samples()
    {
        Gen.Byte.Sample(_ =>
        {
            IMongoDatabase database = GetDatabase();
            MongoDbOutbox outbox = new(database, null, "Outbox");
            return outbox.CommitAsync().GetAwaiter().GetResult().IsSuccess();
        });
    }
}
