namespace SupportFlow.BuildingBlocks.Persistence;

/// <summary>
/// Row of the <c>idempotency_keys</c> table in the schema of the owning module (ADR-0014).
/// </summary>
internal sealed class IdempotencyKey
{
    public IdempotencyKey(
        Guid callerId,
        Guid key,
        string operation,
        byte[] requestHash,
        Guid resourceId,
        DateTimeOffset createdAt)
    {
        CallerId = callerId;
        Key = key;
        Operation = operation;
        RequestHash = requestHash;
        ResourceId = resourceId;
        CreatedAt = createdAt;
    }

    public Guid CallerId { get; private set; }

    public Guid Key { get; private set; }

    public string Operation { get; private set; }

    public byte[] RequestHash { get; private set; }

    public Guid ResourceId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
}
