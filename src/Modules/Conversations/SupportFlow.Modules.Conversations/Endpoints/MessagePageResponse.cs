using SupportFlow.Modules.Conversations.Application;

namespace SupportFlow.Modules.Conversations.Endpoints;

internal sealed record MessagePageResponse(IReadOnlyList<MessageResponse> Items, bool HasMore)
{
    public static MessagePageResponse From(MessagePage page)
    {
        return new MessagePageResponse(page.Items.Select(MessageResponse.From).ToList(), page.HasMore);
    }
}
