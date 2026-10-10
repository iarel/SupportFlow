namespace SupportFlow.BuildingBlocks.Application;

/// <summary>
/// Transactional outbox of integration events (ADR-0003).
/// </summary>
public interface IIntegrationEventOutbox
{
    /// <summary>
    /// Adds the event to the outbox of the current unit of work: it is committed or rolled back together with the
    /// aggregate change. The event is a record from a module's <c>Contracts</c> and carries only business data;
    /// the outbox assigns the event id that consumers deduplicate on (ADR-0008).
    /// </summary>
    void Add<TEvent>(TEvent integrationEvent)
        where TEvent : class;
}
