namespace SupportFlow.Modules.Conversations.Contracts;

/// <summary>
/// Integration event: a customer opened a conversation. Published together with <see cref="MessagePosted"/> for the
/// first message.
/// </summary>
public sealed record ConversationOpened(
    Guid ConversationId,
    Guid CustomerId,
    Guid TeamId,
    DateTimeOffset OccurredAt);
