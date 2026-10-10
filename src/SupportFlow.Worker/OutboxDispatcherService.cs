using SupportFlow.BuildingBlocks.Outbox;

namespace SupportFlow.Worker;

/// <summary>
/// Polls the outbox of every publishing module (ADR-0003). A full batch means more events are waiting, so the next
/// poll starts at once; otherwise it waits for the polling interval. Outbox lag and parked events are measured on
/// their own interval.
/// </summary>
internal sealed partial class OutboxDispatcherService(
    IEnumerable<IOutboxDispatcher> dispatchers,
    OutboxDispatcherOptions options,
    TimeProvider timeProvider,
    ILogger<OutboxDispatcherService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var nextBacklogCheck = timeProvider.GetUtcNow();

        while (!stoppingToken.IsCancellationRequested)
        {
            var moreWaiting = false;

            foreach (var dispatcher in dispatchers)
            {
                try
                {
                    moreWaiting |= await dispatcher.DispatchBatchAsync(stoppingToken) >= options.BatchSize;
                }
                catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
                {
                    // For example the database is unavailable; events stay in the outbox until the next poll.
                    LogPollFailed(logger, exception);
                }
            }

            if (timeProvider.GetUtcNow() >= nextBacklogCheck)
            {
                await ObserveBacklogAsync(stoppingToken);
                nextBacklogCheck = timeProvider.GetUtcNow() + options.BacklogInterval;
            }

            if (!moreWaiting)
            {
                await Task.Delay(options.PollingInterval, timeProvider, stoppingToken);
            }
        }
    }

    private async Task ObserveBacklogAsync(CancellationToken stoppingToken)
    {
        foreach (var dispatcher in dispatchers)
        {
            try
            {
                await dispatcher.ObserveBacklogAsync(stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                LogBacklogFailed(logger, exception);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Polling an outbox failed")]
    private static partial void LogPollFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Measuring an outbox backlog failed")]
    private static partial void LogBacklogFailed(ILogger logger, Exception exception);
}
