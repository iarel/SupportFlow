namespace SupportFlow.BuildingBlocks.Application;

/// <param name="EventId">Assigned by the publisher's outbox; consumers deduplicate on it (ADR-0008, ADR-0015).</param>
/// <param name="CorrelationId">Trace id of the request that published the event; null if it had none.</param>
public sealed record IntegrationEventContext(Guid EventId, string? CorrelationId);
