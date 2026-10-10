using SupportFlow.BuildingBlocks.Domain;

namespace SupportFlow.Modules.Conversations.Domain;

internal sealed record MessagePosted(
    Guid ConversationId,
    Guid MessageId,
    long Seq,
    AuthorType AuthorType,
    Guid? AuthorId,
    DateTimeOffset OccurredAt) : IDomainEvent;
