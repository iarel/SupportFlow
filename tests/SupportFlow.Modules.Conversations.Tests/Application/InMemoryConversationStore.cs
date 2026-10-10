using SupportFlow.BuildingBlocks.Application;
using SupportFlow.Modules.Conversations.Application;
using SupportFlow.Modules.Conversations.Domain;

namespace SupportFlow.Modules.Conversations.Tests.Application;

/// <summary>
/// Repositories, idempotency keys, outbox and unit of work over in-memory collections. Changes, including
/// registered idempotency keys and outbox events, become visible only after a successful commit.
/// </summary>
internal sealed class InMemoryConversationStore
    : IConversationRepository, IMessageRepository, IIdempotencyStore, IIntegrationEventOutbox, IUnitOfWork
{
    private readonly List<Conversation> _pendingConversations = [];
    private readonly List<Message> _pendingMessages = [];
    private readonly Dictionary<IdempotencyScope, IdempotencyRecord> _pendingKeys = [];
    private readonly List<object> _pendingEvents = [];
    private bool _inTransaction;

    public List<Conversation> Conversations { get; } = [];

    public List<Message> Messages { get; } = [];

    public Dictionary<IdempotencyScope, IdempotencyRecord> IdempotencyKeys { get; } = [];

    public List<object> Outbox { get; } = [];

    /// <summary>
    /// Operations performed inside transactions, in order.
    /// </summary>
    public List<string> TransactionLog { get; } = [];

    /// <summary>
    /// Value returned by <see cref="CountOpenedByCustomerSinceAsync"/>.
    /// </summary>
    public int OpenedSinceCount { get; set; }

    /// <summary>
    /// Earliest creation time returned by <see cref="CountOpenedByCustomerSinceAsync"/>.
    /// </summary>
    public DateTimeOffset? EarliestOpenedAt { get; set; }

    public DateTimeOffset? CountedSince { get; private set; }

    /// <summary>
    /// When set, registering this scope behaves as if a concurrent request committed the record first: the
    /// record becomes stored and <see cref="DuplicateIdempotencyKeyException"/> is thrown.
    /// </summary>
    public (IdempotencyScope Scope, IdempotencyRecord Record)? ConcurrentWinner { get; set; }

    public Task<IdempotencyRecord?> FindAsync(IdempotencyScope scope, CancellationToken cancellationToken)
    {
        return Task.FromResult(IdempotencyKeys.GetValueOrDefault(scope));
    }

    public Task RegisterAsync(
        IdempotencyScope scope,
        RequestHash requestHash,
        Guid resourceId,
        CancellationToken cancellationToken)
    {
        EnsureInTransaction();
        TransactionLog.Add(nameof(RegisterAsync));

        if (ConcurrentWinner is { } winner && winner.Scope == scope)
        {
            IdempotencyKeys[winner.Scope] = winner.Record;
            throw new DuplicateIdempotencyKeyException();
        }

        if (IdempotencyKeys.ContainsKey(scope))
        {
            throw new DuplicateIdempotencyKeyException();
        }

        _pendingKeys[scope] = new IdempotencyRecord(requestHash, resourceId);
        return Task.CompletedTask;
    }

    public Task LockCustomerConversationCreationAsync(Guid customerId, CancellationToken cancellationToken)
    {
        EnsureInTransaction();
        TransactionLog.Add(nameof(LockCustomerConversationCreationAsync));
        return Task.CompletedTask;
    }

    public Task<OpenedConversations> CountOpenedByCustomerSinceAsync(
        Guid customerId,
        DateTimeOffset since,
        CancellationToken cancellationToken)
    {
        EnsureInTransaction();
        TransactionLog.Add(nameof(CountOpenedByCustomerSinceAsync));
        CountedSince = since;
        return Task.FromResult(new OpenedConversations(OpenedSinceCount, EarliestOpenedAt));
    }

    public void Add(Conversation conversation)
    {
        _pendingConversations.Add(conversation);
    }

    public void Add(Message message)
    {
        _pendingMessages.Add(message);
    }

    public void Add<TEvent>(TEvent integrationEvent)
        where TEvent : class
    {
        EnsureInTransaction();
        _pendingEvents.Add(integrationEvent);
    }

    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        _inTransaction = true;

        try
        {
            var result = await operation(cancellationToken);

            foreach (var (scope, record) in _pendingKeys)
            {
                IdempotencyKeys[scope] = record;
            }

            Conversations.AddRange(_pendingConversations);
            Messages.AddRange(_pendingMessages);
            Outbox.AddRange(_pendingEvents);
            return result;
        }
        finally
        {
            _pendingKeys.Clear();
            _pendingConversations.Clear();
            _pendingMessages.Clear();
            _pendingEvents.Clear();
            _inTransaction = false;
        }
    }

    private void EnsureInTransaction()
    {
        if (!_inTransaction)
        {
            throw new InvalidOperationException("Must be called inside the unit of work.");
        }
    }
}
