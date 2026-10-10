namespace SupportFlow.BuildingBlocks.Application;

/// <summary>
/// Idempotency keys of HTTP commands, stored in a table of the owning module (ADR-0014).
/// </summary>
public interface IIdempotencyStore
{
    Task<IdempotencyRecord?> FindAsync(IdempotencyScope scope, CancellationToken cancellationToken);

    /// <summary>
    /// Inserts the key immediately, inside the current unit of work. Call it first in the transaction: a
    /// concurrent request with the same scope waits here until the first transaction ends, then gets
    /// <see cref="DuplicateIdempotencyKeyException"/> if the first one committed.
    /// </summary>
    Task RegisterAsync(
        IdempotencyScope scope,
        RequestHash requestHash,
        Guid resourceId,
        CancellationToken cancellationToken);
}
