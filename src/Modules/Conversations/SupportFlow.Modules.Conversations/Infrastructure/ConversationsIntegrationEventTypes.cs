using SupportFlow.BuildingBlocks.Outbox;
using SupportFlow.Modules.Conversations.Contracts;

namespace SupportFlow.Modules.Conversations.Infrastructure;

/// <summary>
/// Stored names of the module's integration events (ADR-0015). Never change a name once events with it may be
/// in the outbox.
/// </summary>
internal static class ConversationsIntegrationEventTypes
{
    public static IntegrationEventTypes All { get; } = new IntegrationEventTypes()
        .Add<ConversationOpened>("conversations.conversation-opened")
        .Add<MessagePosted>("conversations.message-posted");
}
