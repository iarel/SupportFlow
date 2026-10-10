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
        NextAttemptAt = createdAt;
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

    /// <summary>
    /// Delivery attempts started, including one in progress.
    /// </summary>
    public int Attempts { get; private set; }

    public DateTimeOffset NextAttemptAt { get; private set; }

    /// <summary>
    /// Lease of the dispatcher instance delivering the event.
    /// </summary>
    public DateTimeOffset? LockedUntil { get; private set; }

    public DateTimeOffset? ProcessedAt { get; private set; }

    /// <summary>
    /// Set when the attempts are exhausted; the event stays for inspection and manual replay.
    /// </summary>
    public DateTimeOffset? FailedAt { get; private set; }

    public string? LastError { get; private set; }
}
