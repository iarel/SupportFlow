namespace SupportFlow.Modules.Conversations.Application;

internal interface IConversationQueries
{
    Task<ConversationSummary?> FindAsync(Guid conversationId, CancellationToken cancellationToken);
}
