using MongoDB.Driver;
using Xunit;

namespace Robotico.Outbox.MongoDb.Tests;

/// <summary>
/// Tests for MongoDbOutbox: null guard, cancellation, CommitAsync when session is null.
/// </summary>
public sealed class MongoDbOutboxTests
{
    private static IMongoDatabase GetDatabase()
    {
        string connectionString = Environment.GetEnvironmentVariable("ROBOTICO_MONGO_CONNECTION") ?? "mongodb://127.0.0.1:27017";
        MongoClient client = new(connectionString);
        return client.GetDatabase("RoboticoOutboxTests");
    }

    [Fact]
    public async Task EnqueueAsync_throws_ArgumentNullException_when_message_is_null()
    {
        IMongoDatabase database = GetDatabase();
        MongoDbOutbox outbox = new(database, null, "Outbox");

        await Assert.ThrowsAsync<ArgumentNullException>(() => outbox.EnqueueAsync(null!));
    }

    [Fact]
    public async Task EnqueueAsync_throws_OperationCanceledException_when_cancellation_requested()
    {
        IMongoDatabase database = GetDatabase();
        MongoDbOutbox outbox = new(database, null, "Outbox");
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() => outbox.EnqueueAsync(new object(), cts.Token));
    }

    [Fact]
    public async Task CommitAsync_returns_success_when_session_is_null()
    {
        IMongoDatabase database = GetDatabase();
        MongoDbOutbox outbox = new(database, null, "Outbox");

        Robotico.Result.Result result = await outbox.CommitAsync();

        Assert.True(result.IsSuccess());
    }

    [Fact]
    public async Task CommitAsync_throws_OperationCanceledException_when_cancellation_requested()
    {
        IMongoDatabase database = GetDatabase();
        MongoDbOutbox outbox = new(database, null, "Outbox");
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() => outbox.CommitAsync(cts.Token));
    }
}
