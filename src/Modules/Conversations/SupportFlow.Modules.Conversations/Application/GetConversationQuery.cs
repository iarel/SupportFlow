namespace SupportFlow.Modules.Conversations.Application;

/// <summary>
/// FR-003. <paramref name="CustomerId"/> is the authenticated customer.
/// </summary>
internal sealed record GetConversationQuery(Guid ConversationId, Guid CustomerId);
