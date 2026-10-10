using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using SupportFlow.BuildingBlocks.Application;

namespace SupportFlow.BuildingBlocks.Outbox;

/// <summary>
/// Adds integration events to the module's <c>outbox</c> table through its <see cref="DbContext"/>, so they are
/// saved by the same <c>SaveChanges</c> and transaction as the aggregate (ADR-0003, ADR-0015).
/// </summary>
public sealed class EfIntegrationEventOutbox(DbContext context, IntegrationEventTypes types, TimeProvider timeProvider)
    : IIntegrationEventOutbox
{
    public void Add<TEvent>(TEvent integrationEvent)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var now = timeProvider.GetUtcNow();

        context.Set<OutboxMessage>().Add(new OutboxMessage(
            Guid.CreateVersion7(now),
            types.GetName(integrationEvent.GetType()),
            IntegrationEventSerializer.Serialize(integrationEvent),
            now,
            Activity.Current?.Id));
    }
}
