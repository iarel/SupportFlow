namespace SupportFlow.BuildingBlocks.Outbox;

/// <summary>
/// Delivers the outbox of one publishing module. The Worker polls every registered dispatcher.
/// </summary>
public interface IOutboxDispatcher
{
    /// <summary>
    /// Claims and delivers one batch of due events. Returns the number of claimed events.
    /// </summary>
    Task<int> DispatchBatchAsync(CancellationToken cancellationToken);
}
