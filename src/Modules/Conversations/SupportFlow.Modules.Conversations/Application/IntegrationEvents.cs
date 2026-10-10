using SupportFlow.BuildingBlocks.Application;
using SupportFlow.BuildingBlocks.Domain;
using SupportFlow.Modules.Conversations.Domain;

namespace SupportFlow.Modules.Conversations.Application;

/// <summary>
/// Translates domain events of the module into its public integration events (architecture.md §14).
/// </summary>
internal static class IntegrationEvents
{
    /// <summary>
    /// Adds the integration events for the aggregate's domain events to the outbox and clears them. Every domain
    /// event must be mapped explicitly, so a new event cannot be silently left unpublished.
    /// </summary>
    public static void Publish(AggregateRoot aggregate, IIntegrationEventOutbox outbox)
    {
        foreach (var domainEvent in aggregate.DomainEvents)
        {
            switch (domainEvent)
            {
                case ConversationOpened e:
                    outbox.Add(new Contracts.ConversationOpened(e.ConversationId, e.CustomerId, e.TeamId, e.OccurredAt));
                    break;

                case MessagePosted e:
                    outbox.Add(new Contracts.MessagePosted(
                        e.ConversationId,
                        e.MessageId,
                        e.Seq,
                        Map(e.AuthorType),
                        e.AuthorId,
                        e.OccurredAt));
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Domain event {domainEvent.GetType().Name} has no integration event mapping.");
            }
        }

        aggregate.ClearDomainEvents();
    }

    private static Contracts.MessageAuthorType Map(AuthorType authorType)
    {
        return authorType switch
        {
            AuthorType.Customer => Contracts.MessageAuthorType.Customer,
            AuthorType.Agent => Contracts.MessageAuthorType.Agent,
            AuthorType.System => Contracts.MessageAuthorType.System,
            _ => throw new ArgumentOutOfRangeException(nameof(authorType), authorType, null),
        };
    }
}
