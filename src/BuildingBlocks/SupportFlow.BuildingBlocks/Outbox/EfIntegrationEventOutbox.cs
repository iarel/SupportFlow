using System.Text.Json;
using System.Text.Json.Serialization;
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
    private static readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public void Add<TEvent>(TEvent integrationEvent)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var eventType = integrationEvent.GetType();
        var now = timeProvider.GetUtcNow();

        context.Set<OutboxMessage>().Add(new OutboxMessage(
            Guid.CreateVersion7(now),
            types.GetName(eventType),
            JsonSerializer.Serialize(integrationEvent, eventType, _serializerOptions),
            now));
    }
}
