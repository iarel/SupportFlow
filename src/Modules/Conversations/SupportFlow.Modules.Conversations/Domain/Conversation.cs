using SupportFlow.BuildingBlocks.Domain;

namespace SupportFlow.Modules.Conversations.Domain;

/// <summary>
/// Conversation aggregate, domain-model §3.2.
/// </summary>
internal sealed class Conversation : AggregateRoot
{
    /// <summary>
    /// [Assumption] domain-model §3.1.
    /// </summary>
    public const int MaxSubjectLength = 200;

    private Conversation(Guid id, Guid customerId, Guid teamId, string subject, DateTimeOffset createdAt)
    {
        Id = id;
        CustomerId = customerId;
        TeamId = teamId;
        Subject = subject;
        Status = ConversationStatus.New;
        Priority = ConversationPriority.Normal;
        CreatedAt = createdAt;
        LastActivityAt = createdAt;
        Version = 1;
    }

    public Guid Id { get; }

    public Guid CustomerId { get; }

    public Guid TeamId { get; }

    public string Subject { get; }

    public ConversationStatus Status { get; private set; }

    public Guid? AssigneeId { get; private set; }

    public ConversationPriority Priority { get; private set; }

    public int MessageCount { get; private set; }

    public long LastMessageSeq { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset LastActivityAt { get; private set; }

    public int Version { get; private set; }

    /// <summary>
    /// Opens a conversation with its first customer message. Message is a separate aggregate created in the
    /// same transaction (ADR-0004). The id is assigned by the application, because the idempotency key is
    /// registered before the aggregate is created (ADR-0014).
    /// </summary>
    public static (Conversation Conversation, Message FirstMessage) Open(
        Guid id,
        Guid customerId,
        Guid teamId,
        string subject,
        MessageBody firstMessage,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(firstMessage);

        var trimmedSubject = subject?.Trim() ?? string.Empty;

        if (trimmedSubject.Length == 0)
        {
            throw new DomainException("Subject must not be empty.");
        }

        if (trimmedSubject.Length > MaxSubjectLength)
        {
            throw new DomainException($"Subject must not exceed {MaxSubjectLength} characters.");
        }

        var conversation = new Conversation(id, customerId, teamId, trimmedSubject, now);
        var seq = conversation.RegisterFirstMessage();
        var message = Message.Create(conversation.Id, seq, AuthorType.Customer, customerId, firstMessage, now);

        conversation.Raise(new ConversationOpened(conversation.Id, customerId, teamId, now));
        conversation.Raise(new MessagePosted(conversation.Id, message.Id, seq, AuthorType.Customer, customerId, now));

        return (conversation, message);
    }

    private long RegisterFirstMessage()
    {
        MessageCount = 1;
        LastMessageSeq = 1;
        return LastMessageSeq;
    }
}
