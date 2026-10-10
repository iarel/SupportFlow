namespace SupportFlow.BuildingBlocks.Application;

/// <summary>
/// A concurrent request with the same idempotency key committed first (ADR-0014).
/// </summary>
public class DuplicateIdempotencyKeyException : Exception
{
    public DuplicateIdempotencyKeyException()
    {
    }

    public DuplicateIdempotencyKeyException(string message)
        : base(message)
    {
    }

    public DuplicateIdempotencyKeyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
