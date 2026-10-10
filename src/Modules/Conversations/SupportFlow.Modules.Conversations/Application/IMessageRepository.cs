using SupportFlow.Modules.Conversations.Domain;

namespace SupportFlow.Modules.Conversations.Application;

internal interface IMessageRepository
{
    void Add(Message message);
}
