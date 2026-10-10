using System.Text.Json;
using SupportFlow.BuildingBlocks.Application;
using SupportFlow.Modules.Audit.Application;
using SupportFlow.Modules.Audit.Domain;
using SupportFlow.Modules.Conversations.Contracts;

namespace SupportFlow.Modules.Audit.EventHandlers;

/// <summary>
/// A customer opened a conversation (domain-model §8).
/// </summary>
internal sealed class ConversationOpenedAuditHandler(RecordAuditHandler recordAudit)
    : IIntegrationEventHandler<ConversationOpened>
{
    public string Name => "audit.conversation-opened";

    public Task HandleAsync(
        ConversationOpened integrationEvent,
        IntegrationEventContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        ArgumentNullException.ThrowIfNull(context);

        return recordAudit.HandleAsync(
            new RecordAuditCommand(
                Name,
                context.EventId,
                integrationEvent.OccurredAt,
                ActorType.Customer,
                integrationEvent.CustomerId,
                Action: "ConversationOpened",
                TargetType: "Conversation",
                integrationEvent.ConversationId,
                JsonSerializer.Serialize(new { teamId = integrationEvent.TeamId })),
            cancellationToken);
    }
}
