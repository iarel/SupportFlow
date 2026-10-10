namespace SupportFlow.BuildingBlocks.Persistence;

/// <summary>
/// How long delivery and idempotency records are kept (ADR-0003, ADR-0014).
/// </summary>
public sealed class RetentionOptions
{
    /// <summary>
    /// Processed outbox events and inbox records [Assumption]. Parked events are never deleted.
    /// </summary>
    public TimeSpan DeliveredEvents { get; init; } = TimeSpan.FromDays(7);

    /// <summary>
    /// At least 48 hours, ADR-0014 [Assumption].
    /// </summary>
    public TimeSpan IdempotencyKeys { get; init; } = TimeSpan.FromHours(48);

    public int BatchSize { get; init; } = 1000;

    public TimeSpan Interval { get; init; } = TimeSpan.FromHours(1);
}
