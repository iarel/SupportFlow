using SupportFlow.BuildingBlocks.Domain;

namespace SupportFlow.Modules.Audit.Domain;

/// <summary>
/// Append-only record of who did what and when, built from integration events of other modules (domain-model §8).
/// </summary>
internal sealed class AuditEvent : AggregateRoot
{
    private AuditEvent(
        Guid id,
        Guid sourceEventId,
        DateTimeOffset occurredAt,
        ActorType actorType,
        Guid? actorId,
        string action,
        string targetType,
        Guid targetId,
        string? details,
        string? correlationId)
    {
        Id = id;
        SourceEventId = sourceEventId;
        OccurredAt = occurredAt;
        ActorType = actorType;
        ActorId = actorId;
        Action = action;
        TargetType = targetType;
        TargetId = targetId;
        Details = details;
        CorrelationId = correlationId;
    }

    public Guid Id { get; }

    /// <summary>
    /// The integration event recorded; unique, so one event is never recorded twice (ADR-0008).
    /// </summary>
    public Guid SourceEventId { get; }

    public DateTimeOffset OccurredAt { get; }

    public ActorType ActorType { get; }

    public Guid? ActorId { get; }

    public string Action { get; }

    public string TargetType { get; }

    public Guid TargetId { get; }

    /// <summary>
    /// Action-specific data as JSON.
    /// </summary>
    public string? Details { get; }

    public string? CorrelationId { get; }

    public static AuditEvent Record(
        Guid sourceEventId,
        DateTimeOffset occurredAt,
        ActorType actorType,
        Guid? actorId,
        string action,
        string targetType,
        Guid targetId,
        string? details,
        string? correlationId)
    {
        if (string.IsNullOrWhiteSpace(action) || string.IsNullOrWhiteSpace(targetType))
        {
            throw new DomainException("Audit action and target type are required.");
        }

        return new AuditEvent(
            Guid.CreateVersion7(occurredAt),
            sourceEventId,
            occurredAt,
            actorType,
            actorId,
            action,
            targetType,
            targetId,
            details,
            correlationId);
    }
}
