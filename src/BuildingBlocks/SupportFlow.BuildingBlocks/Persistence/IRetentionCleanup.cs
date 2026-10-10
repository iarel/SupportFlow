namespace SupportFlow.BuildingBlocks.Persistence;

/// <summary>
/// Deletes expired delivery and idempotency records of one module. The Worker runs every registered cleanup.
/// </summary>
public interface IRetentionCleanup
{
    /// <summary>
    /// Returns the number of deleted rows.
    /// </summary>
    Task<int> CleanUpAsync(CancellationToken cancellationToken);
}
