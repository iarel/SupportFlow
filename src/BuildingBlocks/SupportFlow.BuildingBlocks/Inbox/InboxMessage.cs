namespace SupportFlow.BuildingBlocks.Inbox;

/// <summary>
/// Row of the <c>inbox</c> table in the schema of the consuming module: the handler handled the event (ADR-0008).
/// </summary>
internal sealed class InboxMessage
{
    public InboxMessage(string handlerName, Guid eventId, DateTimeOffset processedAt)
    {
        HandlerName = handlerName;
        EventId = eventId;
        ProcessedAt = processedAt;
    }

    public string HandlerName { get; private set; }

    public Guid EventId { get; private set; }

    public DateTimeOffset ProcessedAt { get; private set; }
}
