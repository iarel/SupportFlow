using SupportFlow.Modules.Conversations.Application;

namespace SupportFlow.Modules.Conversations.Endpoints;

internal sealed record MessageResponse(Guid Id, long Seq, string AuthorType, string Body, DateTimeOffset CreatedAt)
{
    public static MessageResponse From(MessageView message)
    {
        return new MessageResponse(
            message.Id,
            message.Seq,
            message.AuthorType.ToString(),
            message.Body,
            message.CreatedAt);
    }
}
