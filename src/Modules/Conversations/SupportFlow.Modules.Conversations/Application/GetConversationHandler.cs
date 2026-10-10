namespace SupportFlow.Modules.Conversations.Application;

internal sealed class GetConversationHandler(IConversationQueries conversations)
{
    /// <summary>
    /// Returns null when the conversation does not exist; throws
    /// <see cref="BuildingBlocks.Application.AccessDeniedException"/> when it belongs to another customer.
    /// </summary>
    public async Task<ConversationSummary?> HandleAsync(GetConversationQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var conversation = await conversations.FindAsync(query.ConversationId, cancellationToken);

        if (conversation is not null)
        {
            ConversationAccess.EnsureCustomerOwns(conversation, query.CustomerId);
        }

        return conversation;
    }
}
