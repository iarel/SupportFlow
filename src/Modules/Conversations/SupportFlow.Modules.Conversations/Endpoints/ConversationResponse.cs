using SupportFlow.Modules.Conversations.Application;

namespace SupportFlow.Modules.Conversations.Endpoints;

internal sealed record ConversationResponse(
    Guid Id,
    string Subject,
    string Status,
    string Priority,
    DateTimeOffset CreatedAt)
{
    public static ConversationResponse From(ConversationSummary conversation)
    {
        return new ConversationResponse(
            conversation.Id,
            conversation.Subject,
            conversation.Status.ToString(),
            conversation.Priority.ToString(),
            conversation.CreatedAt);
    }
}
