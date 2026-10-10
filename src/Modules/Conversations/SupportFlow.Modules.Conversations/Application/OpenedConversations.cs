namespace SupportFlow.Modules.Conversations.Application;

/// <summary>
/// Conversations a customer opened within the daily limit window (ADR-0012).
/// </summary>
/// <param name="Count">Conversations created within the window.</param>
/// <param name="EarliestCreatedAt">The first to leave the window; null when <paramref name="Count"/> is 0.</param>
internal sealed record OpenedConversations(int Count, DateTimeOffset? EarliestCreatedAt)
{
    public static readonly OpenedConversations None = new(0, null);
}
