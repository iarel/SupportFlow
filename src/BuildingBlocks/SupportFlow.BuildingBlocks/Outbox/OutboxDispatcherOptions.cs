namespace SupportFlow.BuildingBlocks.Outbox;

/// <summary>
/// Delivery settings of the outbox dispatcher (ADR-0003). Attempt limit and delays are [Assumption].
/// </summary>
public sealed class OutboxDispatcherOptions
{
    public int BatchSize { get; init; } = 50;

    /// <summary>
    /// Pause between polls when the last batch was not full.
    /// </summary>
    public TimeSpan PollingInterval { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Lease of a claimed batch; after it another instance may deliver the events.
    /// </summary>
    public TimeSpan LockDuration { get; init; } = TimeSpan.FromSeconds(30);

    public int MaxAttempts { get; init; } = 10;

    public TimeSpan InitialRetryDelay { get; init; } = TimeSpan.FromSeconds(1);

    public TimeSpan MaxRetryDelay { get; init; } = TimeSpan.FromMinutes(5);
}
