namespace SupportFlow.Modules.Conversations.Application;

internal sealed class GetMessagesHandler(IConversationQueries conversations, IMessageQueries messages)
{
    /// <summary>
    /// Page size, components.md §1.3 [Assumption].
    /// </summary>
    public const int DefaultLimit = 50;

    public const int MaxLimit = 200;

    /// <summary>
    /// Returns null when the conversation does not exist; throws
    /// <see cref="BuildingBlocks.Application.AccessDeniedException"/> when it belongs to another customer.
    /// </summary>
    public async Task<MessagePage?> HandleAsync(GetMessagesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentOutOfRangeException.ThrowIfLessThan(query.Limit, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(query.Limit, MaxLimit);

        if (query.AfterSeq is not null && query.BeforeSeq is not null)
        {
            throw new ArgumentException("Only one of afterSeq and beforeSeq can be set.", nameof(query));
        }

        var conversation = await conversations.FindAsync(query.ConversationId, cancellationToken);

        if (conversation is null)
        {
            return null;
        }

        ConversationAccess.EnsureCustomerOwns(conversation, query.CustomerId);

        return await messages.GetPageAsync(
            query.ConversationId,
            query.AfterSeq,
            query.BeforeSeq,
            query.Limit,
            cancellationToken);
    }
}
