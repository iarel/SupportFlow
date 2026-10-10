namespace SupportFlow.BuildingBlocks.Application;

/// <summary>
/// The command waited too long for a lock held by a concurrent transaction and was rolled back. Safe to retry
/// with the same idempotency key (ADR-0014). Maps to 503 with <c>Retry-After</c>.
/// </summary>
public class LockTimeoutException : Exception
{
    public LockTimeoutException()
    {
    }

    public LockTimeoutException(string message)
        : base(message)
    {
    }

    public LockTimeoutException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
