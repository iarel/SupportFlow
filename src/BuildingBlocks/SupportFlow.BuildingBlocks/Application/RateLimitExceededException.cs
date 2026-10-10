namespace SupportFlow.BuildingBlocks.Application;

/// <summary>
/// A limit checked by the application was exceeded (ADR-0012). Maps to 429 with <c>Retry-After</c>.
/// </summary>
public class RateLimitExceededException : Exception
{
    public RateLimitExceededException()
    {
    }

    public RateLimitExceededException(string message)
        : base(message)
    {
    }

    public RateLimitExceededException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public RateLimitExceededException(string message, TimeSpan retryAfter)
        : base(message)
    {
        RetryAfter = retryAfter;
    }

    /// <summary>
    /// When the request can succeed again; null if unknown.
    /// </summary>
    public TimeSpan? RetryAfter { get; }
}
