using SupportFlow.BuildingBlocks.Domain;

namespace SupportFlow.Modules.Conversations.Domain;

internal sealed record ConversationOpened(
    Guid ConversationId,
    Guid CustomerId,
    Guid TeamId,
    DateTimeOffset OccurredAt) : IDomainEvent;
