using Microsoft.EntityFrameworkCore;
using Npgsql;
using SupportFlow.BuildingBlocks.Application;

namespace SupportFlow.BuildingBlocks.Persistence;

/// <summary>
/// One database transaction of the module's <see cref="DbContext"/> per command (architecture.md §6).
/// </summary>
public sealed class EfUnitOfWork(DbContext context) : IUnitOfWork
{
    /// <summary>
    /// Bounds waiting for the idempotency key, the customer lock or a row lock held by a concurrent transaction
    /// (ADR-0014). Without it PostgreSQL waits indefinitely.
    /// </summary>
    private const string SetLockTimeout = "SET LOCAL lock_timeout = '5s'";

    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        // Disposing an uncommitted transaction rolls it back.
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await context.Database.ExecuteSqlRawAsync(SetLockTimeout, cancellationToken);

            var result = await operation(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch (Exception exception) when (IsLockTimeout(exception))
        {
            context.ChangeTracker.Clear();
            throw new LockTimeoutException("A concurrent transaction held a lock for too long.", exception);
        }
        catch
        {
            // Nothing of the failed command may be saved by a later SaveChanges of the same context.
            context.ChangeTracker.Clear();
            throw;
        }
    }

    private static bool IsLockTimeout(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException { SqlState: PostgresErrorCodes.LockNotAvailable })
            {
                return true;
            }
        }

        return false;
    }
}
