using SupportFlow.Modules.Audit.Domain;

namespace SupportFlow.Modules.Audit.Application;

/// <summary>
/// Records an integration event of another module. <see cref="HandlerName"/> is the integration event handler
/// delivering it, for the inbox (ADR-0008); <see cref="SourceEventId"/> is the event's id.
/// </summary>
internal sealed record RecordAuditCommand(
    string HandlerName,
    Guid SourceEventId,
    DateTimeOffset OccurredAt,
    ActorType ActorType,
    Guid? ActorId,
    string Action,
    string TargetType,
    Guid TargetId,
    string? Details,
    string? CorrelationId);
