using Microsoft.EntityFrameworkCore;
using Npgsql;
using SupportFlow.BuildingBlocks.Application;

namespace SupportFlow.BuildingBlocks.Persistence;

/// <summary>
/// Idempotency keys in the module's <c>idempotency_keys</c> table (ADR-0014). The context must include
/// <see cref="ModelBuilderExtensions.ApplyIdempotencyKeys"/>.
/// </summary>
public sealed class EfIdempotencyStore(DbContext context, TimeProvider timeProvider) : IIdempotencyStore
{
    public async Task<IdempotencyRecord?> FindAsync(IdempotencyScope scope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scope);

        var key = await context.Set<IdempotencyKey>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                k => k.CallerId == scope.CallerId && k.Key == scope.Key && k.Operation == scope.Operation,
                cancellationToken);

        return key is null
            ? null
            : new IdempotencyRecord(RequestHash.FromBytes(key.RequestHash), key.ResourceId);
    }

    /// <summary>
    /// Saves the key at once: a concurrent transaction that inserted the same key makes this call wait until it
    /// ends. Must be the first change in the unit of work, since saving also flushes pending changes.
    /// </summary>
    public async Task RegisterAsync(
        IdempotencyScope scope,
        RequestHash requestHash,
        Guid resourceId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(requestHash);

        context.Set<IdempotencyKey>().Add(new IdempotencyKey(
            scope.CallerId,
            scope.Key,
            scope.Operation,
            requestHash.Value.ToArray(),
            resourceId,
            timeProvider.GetUtcNow()));

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new DuplicateIdempotencyKeyException(
                "A concurrent request with the same idempotency key committed first.",
                exception);
        }
    }
}
