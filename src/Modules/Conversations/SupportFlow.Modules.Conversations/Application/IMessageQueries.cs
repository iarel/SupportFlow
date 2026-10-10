namespace SupportFlow.Modules.Conversations.Application;

internal interface IMessageQueries
{
    /// <summary>
    /// Keyset page by <c>Seq</c> (components.md §1.3): messages after <paramref name="afterSeq"/>, before
    /// <paramref name="beforeSeq"/>, or the latest ones when both are null. At most one of them is set.
    /// </summary>
    Task<MessagePage> GetPageAsync(
        Guid conversationId,
        long? afterSeq,
        long? beforeSeq,
        int limit,
        CancellationToken cancellationToken);
}
