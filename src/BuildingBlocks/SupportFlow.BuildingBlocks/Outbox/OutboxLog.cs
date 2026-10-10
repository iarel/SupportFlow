using Microsoft.Extensions.Logging;

namespace SupportFlow.BuildingBlocks.Outbox;

internal static partial class OutboxLog
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Delivery of integration event {EventId} ({EventType}) failed on attempt {Attempts}; next attempt at {NextAttemptAt}")]
    public static partial void WillRetry(
        ILogger logger,
        Exception exception,
        Guid eventId,
        string eventType,
        int attempts,
        DateTimeOffset nextAttemptAt);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Delivery of integration event {EventId} ({EventType}) failed {Attempts} times and is parked")]
    public static partial void Parked(ILogger logger, Exception exception, Guid eventId, string eventType, int attempts);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Lease of integration event {EventId} ({EventType}) expired during delivery; another instance redelivers it")]
    public static partial void LeaseLost(ILogger logger, Guid eventId, string eventType);
}
