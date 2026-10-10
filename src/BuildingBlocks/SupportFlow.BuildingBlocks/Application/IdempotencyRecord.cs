namespace SupportFlow.BuildingBlocks.Application;

/// <summary>
/// A committed idempotent command: the hash of its request and the resource it created (ADR-0014).
/// </summary>
public sealed record IdempotencyRecord(RequestHash RequestHash, Guid ResourceId)
{
    /// <summary>
    /// Returns the created resource for a repeated request, or throws when the key was used with a different
    /// request.
    /// </summary>
    public Guid ResolveReplay(RequestHash requestHash)
    {
        return requestHash == RequestHash
            ? ResourceId
            : throw new IdempotencyKeyReusedException(
                "The idempotency key was already used with a different request.");
    }
}
