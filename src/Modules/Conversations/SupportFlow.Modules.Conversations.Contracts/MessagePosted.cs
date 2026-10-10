namespace SupportFlow.Modules.Conversations.Contracts;

/// <summary>
/// Integration event: a message was added to a conversation, including the first message on open. Carries no body;
/// consumers read messages through queries.
/// </summary>
public sealed record MessagePosted(
    Guid ConversationId,
    Guid MessageId,
    long Seq,
    MessageAuthorType AuthorType,
    Guid? AuthorId,
    DateTimeOffset OccurredAt);
