using SupportFlow.BuildingBlocks.Persistence;

namespace SupportFlow.Worker;

/// <summary>
/// Deletes expired outbox, inbox and idempotency records of every module on an interval (ADR-0003, ADR-0014).
/// </summary>
internal sealed partial class RetentionCleanupService(
    IEnumerable<IRetentionCleanup> cleanups,
    RetentionOptions options,
    TimeProvider timeProvider,
    ILogger<RetentionCleanupService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Interval, timeProvider);

        do
        {
            foreach (var cleanup in cleanups)
            {
                try
                {
                    var deleted = await cleanup.CleanUpAsync(stoppingToken);
                    LogCleanedUp(logger, deleted);
                }
                catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
                {
                    LogCleanupFailed(logger, exception);
                }
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Retention cleanup deleted {Deleted} rows")]
    private static partial void LogCleanedUp(ILogger logger, int deleted);

    [LoggerMessage(Level = LogLevel.Error, Message = "Retention cleanup failed")]
    private static partial void LogCleanupFailed(ILogger logger, Exception exception);
}
