namespace SupportFlow.BuildingBlocks.Outbox;

/// <summary>
/// Row of the <c>outbox</c> table in the schema of the publishing module (ADR-0003). <see cref="Id"/> is the
/// event id that consumers deduplicate on (ADR-0008, ADR-0015).
/// </summary>
public sealed class OutboxMessage
{
    public OutboxMessage(Guid id, string type, string payload, DateTimeOffset createdAt)
    {
        Id = id;
        Type = type;
        Payload = payload;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    /// <summary>
    /// Stable event name from <see cref="IntegrationEventTypes"/>, not the CLR type name.
    /// </summary>
    public string Type { get; private set; }

    /// <summary>
    /// The integration event as JSON.
    /// </summary>
    public string Payload { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ProcessedAt { get; private set; }
}
