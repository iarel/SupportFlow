namespace SupportFlow.Modules.Conversations.Application;

/// <param name="Items">Messages ascending by <c>Seq</c> (ADR-0005).</param>
/// <param name="HasMore">More messages exist in the requested direction: older for the latest page and
/// <c>beforeSeq</c>, newer for <c>afterSeq</c>.</param>
internal sealed record MessagePage(IReadOnlyList<MessageView> Items, bool HasMore);
