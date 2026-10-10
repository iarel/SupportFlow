using System.Text.Json;
using System.Text.Json.Serialization;

namespace SupportFlow.BuildingBlocks.Outbox;

/// <summary>
/// JSON of integration events in the outbox: camelCase, enums as names, so payloads stay readable and survive
/// reordering of enum members.
/// </summary>
internal static class IntegrationEventSerializer
{
    private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static string Serialize(object integrationEvent)
    {
        return JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), _options);
    }

    public static object Deserialize(string payload, Type eventType)
    {
        return JsonSerializer.Deserialize(payload, eventType, _options)
            ?? throw new InvalidOperationException($"The payload of {eventType.Name} is empty.");
    }
}
