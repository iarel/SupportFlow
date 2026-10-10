namespace SupportFlow.BuildingBlocks.Application;

/// <summary>
/// The idempotency key was already used with a different request (ADR-0014). Maps to 422.
/// </summary>
public class IdempotencyKeyReusedException : Exception
{
    public IdempotencyKeyReusedException()
    {
    }

    public IdempotencyKeyReusedException(string message)
        : base(message)
    {
    }

    public IdempotencyKeyReusedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
