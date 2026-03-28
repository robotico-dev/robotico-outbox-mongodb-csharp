namespace Robotico.Outbox.MongoDb.Tests;

/// <summary>Serializable payload for MongoDbOutbox integration tests.</summary>
public sealed class OutboxTestMessage
{
    public int Id { get; set; }

    public string Name { get; set; } = "";
}
