using SupportFlow.Modules.Conversations.Application;
using SupportFlow.Modules.Conversations.Domain;

namespace SupportFlow.Modules.Conversations.Infrastructure;

internal sealed class MessageRepository(ConversationsDbContext context) : IMessageRepository
{
    public void Add(Message message)
    {
        context.Messages.Add(message);
    }
}
