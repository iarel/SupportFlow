namespace SupportFlow.Modules.Conversations.Application;

/// <summary>
/// FR-004. <paramref name="CustomerId"/> is the authenticated customer; at most one of <paramref name="AfterSeq"/>
/// and <paramref name="BeforeSeq"/> is set.
/// </summary>
internal sealed record GetMessagesQuery(
    Guid ConversationId,
    Guid CustomerId,
    long? AfterSeq,
    long? BeforeSeq,
    int Limit);
