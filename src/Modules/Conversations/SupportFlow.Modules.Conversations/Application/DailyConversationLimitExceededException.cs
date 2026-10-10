using SupportFlow.BuildingBlocks.Application;

namespace SupportFlow.Modules.Conversations.Application;

/// <summary>
/// The customer opened the maximum number of conversations in the last 24 hours (requirements §4.7, ADR-0012).
/// </summary>
internal sealed class DailyConversationLimitExceededException : RateLimitExceededException
{
    public DailyConversationLimitExceededException()
    {
    }

    public DailyConversationLimitExceededException(string message)
        : base(message)
    {
    }

    public DailyConversationLimitExceededException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public DailyConversationLimitExceededException(string message, TimeSpan retryAfter)
        : base(message, retryAfter)
    {
    }
}
