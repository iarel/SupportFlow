namespace SupportFlow.Modules.Conversations.Domain;

/// <summary>
/// Immutable message of a conversation. A separate aggregate (ADR-0004). Raises no domain events:
/// <see cref="MessagePosted"/> is raised by <see cref="Conversation"/>, which assigns the <c>Seq</c> (ADR-0015).
/// </summary>
internal sealed class Message
{
    private Message(
        Guid id,
        Guid conversationId,
        long seq,
        AuthorType authorType,
        Guid? authorId,
        MessageBody body,
        DateTimeOffset createdAt)
    {
        Id = id;
        ConversationId = conversationId;
        Seq = seq;
        AuthorType = authorType;
        AuthorId = authorId;
        Body = body;
        CreatedAt = createdAt;
    }

    public Guid Id { get; }

    public Guid ConversationId { get; }

    public long Seq { get; }

    public AuthorType AuthorType { get; }

    public Guid? AuthorId { get; }

    public MessageBody Body { get; }

    public DateTimeOffset CreatedAt { get; }

    internal static Message Create(
        Guid conversationId,
        long seq,
        AuthorType authorType,
        Guid? authorId,
        MessageBody body,
        DateTimeOffset createdAt)
    {
        return new Message(
            Guid.CreateVersion7(createdAt),
            conversationId,
            seq,
            authorType,
            authorId,
            body,
            createdAt);
    }
}
