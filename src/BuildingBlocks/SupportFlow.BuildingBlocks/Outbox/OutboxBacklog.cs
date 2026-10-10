namespace SupportFlow.BuildingBlocks.Outbox;

/// <param name="Lag">Age of the oldest undelivered event; zero when nothing is waiting.</param>
/// <param name="Parked">Events parked after exhausting their attempts.</param>
public sealed record OutboxBacklog(TimeSpan Lag, long Parked);
