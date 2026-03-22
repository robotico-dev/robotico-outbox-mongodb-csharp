using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using Robotico.Result.Errors;

namespace Robotico.Outbox.MongoDb;

/// <summary>
/// MongoDB implementation of <see cref="IOutbox"/> that stores messages in a collection.
/// </summary>
/// <remarks>
/// <para>Use the same <see cref="IClientSessionHandle"/> for domain operations and this outbox so that <see cref="CommitAsync"/> commits one transaction including both outbox inserts and domain writes.</para>
/// <para>Start the session and transaction before enqueueing; then call <see cref="CommitAsync"/> to persist.</para>
/// </remarks>
public sealed class MongoDbOutbox(IMongoDatabase database, IClientSessionHandle? session = null, string collectionName = "Outbox") : Robotico.Outbox.IOutbox
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };
    private readonly IMongoCollection<BsonDocument> _collection = database.GetCollection<BsonDocument>(collectionName);

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/> is null.</exception>
    public async Task<Robotico.Result.Result> EnqueueAsync(object message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            string payload = JsonSerializer.Serialize(message, JsonOptions);
            string messageType = message.GetType().AssemblyQualifiedName ?? message.GetType().FullName ?? "Unknown";
            BsonDocument doc = new()
            {
                ["_id"] = ObjectId.GenerateNewId(),
                ["MessageType"] = messageType,
                ["Payload"] = payload,
                ["CreatedAtUtc"] = DateTime.UtcNow
            };
            if (session is null)
            {
                await _collection.InsertOneAsync(doc, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await _collection.InsertOneAsync(session, doc, cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            return Robotico.Result.Result.Success();
        }
        catch (MongoException ex)
        {
            return Robotico.Result.Result.Error(new ExceptionError(ex));
        }
    }

    /// <inheritdoc />
    public async Task<Robotico.Result.Result> CommitAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (session is null)
        {
            return Robotico.Result.Result.Success();
        }

        try
        {
            await session.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
            return Robotico.Result.Result.Success();
        }
        catch (MongoException ex)
        {
            return Robotico.Result.Result.Error(new ExceptionError(ex));
        }
    }
}
