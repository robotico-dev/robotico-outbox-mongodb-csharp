using MongoDB.Bson;
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
    public async Task EnqueueAsync_without_session_inserts_document()
    {
        IMongoDatabase database = GetDatabase();
        string collectionName = "outbox_" + Guid.NewGuid().ToString("N");
        MongoDbOutbox outbox = new(database, null, collectionName);
        OutboxTestMessage message = new() { Id = 42, Name = "test" };

        Robotico.Result.Result result = await outbox.EnqueueAsync(message);

        Assert.True(result.IsSuccess());
        long count = await database.GetCollection<BsonDocument>(collectionName)
            .CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task EnqueueAsync_with_session_and_CommitAsync_persists_after_transaction()
    {
        string connectionString = Environment.GetEnvironmentVariable("ROBOTICO_MONGO_CONNECTION") ?? "mongodb://127.0.0.1:27017";
        MongoClient client = new(connectionString);
        IMongoDatabase database = client.GetDatabase("RoboticoOutboxTests");
        string collectionName = "outbox_sess_" + Guid.NewGuid().ToString("N");
        IClientSessionHandle session = await client.StartSessionAsync();
        try
        {
            session.StartTransaction();
            MongoDbOutbox outbox = new(database, session, collectionName);
            OutboxTestMessage message = new() { Id = 7, Name = "tx" };

            Robotico.Result.Result enqueue = await outbox.EnqueueAsync(message);
            Assert.True(enqueue.IsSuccess());
            Robotico.Result.Result commit = await outbox.CommitAsync();
            Assert.True(commit.IsSuccess());

            long count = await database.GetCollection<BsonDocument>(collectionName)
                .CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
            Assert.Equal(1, count);
        }
        finally
        {
            session.Dispose();
        }
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
