using SupportFlow.Modules.Conversations.Domain;

namespace SupportFlow.Modules.Conversations.Application;

internal interface IConversationRepository
{
    /// <summary>
    /// Serializes conversation creation of one customer until the unit of work ends
    /// (<c>pg_advisory_xact_lock</c>, ADR-0012). Must be called inside the unit of work, after the idempotency key
    /// is registered (ADR-0014), and before <see cref="CountOpenedByCustomerSinceAsync"/>.
    /// </summary>
    Task LockCustomerConversationCreationAsync(Guid customerId, CancellationToken cancellationToken);

    Task<OpenedConversations> CountOpenedByCustomerSinceAsync(
        Guid customerId,
        DateTimeOffset since,
        CancellationToken cancellationToken);

    void Add(Conversation conversation);
}
