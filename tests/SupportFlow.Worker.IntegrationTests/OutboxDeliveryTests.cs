using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SupportFlow.BuildingBlocks.Application;
using SupportFlow.BuildingBlocks.Outbox;
using SupportFlow.BuildingBlocks.Persistence;
using SupportFlow.Modules.Conversations.Application;
using SupportFlow.Modules.Conversations.Contracts;

namespace SupportFlow.Worker.IntegrationTests;

/// <summary>
/// Delivery of ConversationOpened from the Conversations outbox to the Audit module (ADR-0003, ADR-0008) and
/// retention cleanup, against PostgreSQL. A dispatcher delivers every due event of the shared outbox, so these
/// tests stay in one class, which xunit runs one test at a time.
/// </summary>
public sealed class OutboxDeliveryTests(WorkerFixture database) : IAsyncDisposable
{
    private const string ConversationOpenedType = "conversations.conversation-opened";

    private static readonly OutboxDispatcherOptions _fastRetries = new()
    {
        MaxAttempts = 3,
        InitialRetryDelay = TimeSpan.Zero,
    };

    private readonly ServiceProvider _services = database.CreateServices(_fastRetries);
    private readonly Guid _customerId = Guid.NewGuid();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public ValueTask DisposeAsync()
    {
        return _services.DisposeAsync();
    }

    [Fact]
    public async Task DeliversConversationOpenedToAudit()
    {
        var conversationId = await OpenConversationAsync();

        await DispatchAllAsync(_services);

        var opened = await OutboxRowAsync(conversationId, ConversationOpenedType);
        Assert.NotNull(opened.ProcessedAt);
        Assert.Equal(1, opened.Attempts);
        Assert.Equal(
            [(opened.Id, "Customer", _customerId, "ConversationOpened")],
            await AuditEventsOfAsync(conversationId));

        // MessagePosted has no handler yet and is still marked delivered.
        Assert.NotNull((await OutboxRowAsync(conversationId, "conversations.message-posted")).ProcessedAt);
    }

    [Fact]
    public async Task RedeliveredEventIsRecordedOnce()
    {
        var conversationId = await OpenConversationAsync();
        await DispatchAllAsync(_services);
        var opened = await OutboxRowAsync(conversationId, ConversationOpenedType);

        // As if the dispatcher crashed after the handlers ran but before marking the event processed.
        await ExecuteAsync("UPDATE conversations.outbox SET processed_at = NULL WHERE id = $1", opened.Id);
        await DispatchAllAsync(_services);

        Assert.NotNull((await OutboxRowAsync(conversationId, ConversationOpenedType)).ProcessedAt);
        Assert.Single(await AuditEventsOfAsync(conversationId));
    }

    [Fact]
    public async Task FailingHandlerIsRetriedAndEventIsParkedAfterMaxAttempts()
    {
        await using var services = database.CreateServices(
            _fastRetries,
            s => s.AddIntegrationEventHandler<ConversationOpened, FailingHandler>());
        var conversationId = await OpenConversationAsync();

        await DispatchAllAsync(services);

        var opened = await OutboxRowAsync(conversationId, ConversationOpenedType);
        Assert.Equal(_fastRetries.MaxAttempts, opened.Attempts);
        Assert.NotNull(opened.FailedAt);
        Assert.Null(opened.ProcessedAt);
        Assert.Contains(FailingHandler.Error, opened.LastError, StringComparison.Ordinal);

        // The audit handler succeeded on the first attempt and skipped the retries by its inbox.
        Assert.Single(await AuditEventsOfAsync(conversationId));
    }

    [Fact]
    public async Task LeasedEventIsDeliveredOnlyAfterLeaseExpires()
    {
        var conversationId = await OpenConversationAsync();
        var opened = await OutboxRowAsync(conversationId, ConversationOpenedType);

        await ExecuteAsync("UPDATE conversations.outbox SET locked_until = now() + interval '1 hour' WHERE id = $1", opened.Id);
        await DispatchAllAsync(_services);
        Assert.Empty(await AuditEventsOfAsync(conversationId));

        await ExecuteAsync("UPDATE conversations.outbox SET locked_until = now() - interval '1 second' WHERE id = $1", opened.Id);
        await DispatchAllAsync(_services);
        Assert.Single(await AuditEventsOfAsync(conversationId));
    }

    [Fact]
    public async Task ConcurrentDispatchersClaimEachEventOnce()
    {
        var conversationId = await OpenConversationAsync();
        await using var otherInstance = database.CreateServices(_fastRetries);

        await Task.WhenAll(
            Task.Run(() => DispatchAllAsync(_services), CancellationToken),
            Task.Run(() => DispatchAllAsync(otherInstance), CancellationToken));

        Assert.Equal(1, (await OutboxRowAsync(conversationId, ConversationOpenedType)).Attempts);
        Assert.Single(await AuditEventsOfAsync(conversationId));
    }

    [Fact]
    public async Task DeliveryContinuesTraceOfPublishingRequest()
    {
        using var requests = new ActivitySource("SupportFlow.Tests");
        var deliveries = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name is "SupportFlow.Tests" or "SupportFlow.Outbox",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                if (activity.Source.Name == "SupportFlow.Outbox")
                {
                    lock (deliveries)
                    {
                        deliveries.Add(activity);
                    }
                }
            },
        };
        ActivitySource.AddActivityListener(listener);

        Guid conversationId;
        ActivityTraceId traceId;

        using (var request = requests.StartActivity("POST /conversations"))
        {
            Assert.NotNull(request);
            traceId = request.TraceId;
            conversationId = await OpenConversationAsync();
        }

        await DispatchAllAsync(_services);

        Assert.Contains(
            deliveries,
            d => d.TraceId == traceId && d.OperationName == $"deliver {ConversationOpenedType}");
        Assert.Equal(traceId.ToHexString(), await AuditCorrelationIdAsync(conversationId));
    }

    [Fact]
    public async Task BacklogReportsOldestPendingEventAndParkedEvents()
    {
        var pending = await InsertOutboxRowAsync("created_at = now() - interval '2 minutes'");
        var parked = await InsertOutboxRowAsync("failed_at = now()");

        try
        {
            var backlog = await _services.GetServices<IOutboxDispatcher>().Single().ObserveBacklogAsync(CancellationToken);

            Assert.True(backlog.Lag >= TimeSpan.FromMinutes(2), $"Lag was {backlog.Lag}.");
            Assert.True(backlog.Parked >= 1, $"Parked was {backlog.Parked}.");
        }
        finally
        {
            // The test rows have an unknown event type; other tests must not deliver them.
            await ExecuteAsync("DELETE FROM conversations.outbox WHERE id = $1", pending);
            await ExecuteAsync("DELETE FROM conversations.outbox WHERE id = $1", parked);
        }
    }

    [Fact]
    public async Task CleanupDeletesOnlyExpiredRecords()
    {
        var oldProcessed = await InsertOutboxRowAsync("processed_at = now() - interval '8 days'");
        var recentProcessed = await InsertOutboxRowAsync("processed_at = now() - interval '1 day'");
        var oldParked = await InsertOutboxRowAsync("failed_at = now() - interval '30 days'");
        var oldInbox = await InsertInboxRowAsync("8 days");
        var recentInbox = await InsertInboxRowAsync("1 day");
        var oldKey = await InsertIdempotencyKeyAsync("3 days");
        var recentKey = await InsertIdempotencyKeyAsync("1 hour");

        foreach (var cleanup in _services.GetServices<IRetentionCleanup>())
        {
            await cleanup.CleanUpAsync(CancellationToken);
        }

        Assert.False(await ExistsAsync("conversations.outbox", "id", oldProcessed));
        Assert.True(await ExistsAsync("conversations.outbox", "id", recentProcessed));
        Assert.True(await ExistsAsync("conversations.outbox", "id", oldParked));
        Assert.False(await ExistsAsync("audit.inbox", "event_id", oldInbox));
        Assert.True(await ExistsAsync("audit.inbox", "event_id", recentInbox));
        Assert.False(await ExistsAsync("conversations.idempotency_keys", "key", oldKey));
        Assert.True(await ExistsAsync("conversations.idempotency_keys", "key", recentKey));
    }

    private static async Task DispatchAllAsync(IServiceProvider services)
    {
        foreach (var dispatcher in services.GetServices<IOutboxDispatcher>())
        {
            while (await dispatcher.DispatchBatchAsync(CancellationToken) > 0)
            {
            }
        }
    }

    private async Task<Guid> OpenConversationAsync()
    {
        await using var scope = _services.CreateAsyncScope();
        var result = await scope.ServiceProvider.GetRequiredService<OpenConversationHandler>().HandleAsync(
            new OpenConversationCommand(_customerId, Guid.NewGuid(), "Payment failed", "My card was charged twice."),
            CancellationToken);

        return result.ConversationId;
    }

    private async Task<OutboxRow> OutboxRowAsync(Guid conversationId, string type)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = Command(
            connection,
            """
            SELECT id, attempts, processed_at, failed_at, last_error FROM conversations.outbox
            WHERE type = $1 AND payload->>'conversationId' = $2
            """,
            type,
            conversationId.ToString());
        await using var reader = await command.ExecuteReaderAsync(CancellationToken);

        Assert.True(await reader.ReadAsync(CancellationToken));

        return new OutboxRow(
            reader.GetGuid(0),
            reader.GetInt32(1),
            await reader.IsDBNullAsync(2, CancellationToken) ? null : reader.GetFieldValue<DateTimeOffset>(2),
            await reader.IsDBNullAsync(3, CancellationToken) ? null : reader.GetFieldValue<DateTimeOffset>(3),
            await reader.IsDBNullAsync(4, CancellationToken) ? null : reader.GetString(4));
    }

    private async Task<List<(Guid SourceEventId, string ActorType, Guid ActorId, string Action)>> AuditEventsOfAsync(
        Guid conversationId)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = Command(
            connection,
            "SELECT source_event_id, actor_type, actor_id, action FROM audit.audit_events WHERE target_id = $1",
            conversationId);
        await using var reader = await command.ExecuteReaderAsync(CancellationToken);

        var events = new List<(Guid, string, Guid, string)>();

        while (await reader.ReadAsync(CancellationToken))
        {
            events.Add((reader.GetGuid(0), reader.GetString(1), reader.GetGuid(2), reader.GetString(3)));
        }

        return events;
    }

    private async Task<string?> AuditCorrelationIdAsync(Guid conversationId)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = Command(
            connection,
            "SELECT correlation_id FROM audit.audit_events WHERE target_id = $1",
            conversationId);
        return await command.ExecuteScalarAsync(CancellationToken) as string;
    }

    private async Task<Guid> InsertOutboxRowAsync(string state)
    {
        var id = Guid.NewGuid();
        await ExecuteAsync(
            $$"""
            INSERT INTO conversations.outbox (id, type, payload, created_at, attempts, next_attempt_at)
            VALUES ($1, 'test', '{}', now(), 1, now());
            UPDATE conversations.outbox SET {{state}} WHERE id = $1
            """,
            id);
        return id;
    }

    private async Task<Guid> InsertInboxRowAsync(string age)
    {
        var eventId = Guid.NewGuid();
        await ExecuteAsync(
            $"INSERT INTO audit.inbox (handler_name, event_id, processed_at) VALUES ('test', $1, now() - interval '{age}')",
            eventId);
        return eventId;
    }

    private async Task<Guid> InsertIdempotencyKeyAsync(string age)
    {
        var key = Guid.NewGuid();
        await ExecuteAsync(
            $"""
            INSERT INTO conversations.idempotency_keys (caller_id, key, operation, request_hash, resource_id, created_at)
            VALUES ($1, $1, 'test', '\x00'::bytea, $1, now() - interval '{age}')
            """,
            key);
        return key;
    }

    private async Task<bool> ExistsAsync(string table, string column, Guid value)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = Command(connection, $"SELECT EXISTS (SELECT 1 FROM {table} WHERE {column} = $1)", value);
        return (bool)(await command.ExecuteScalarAsync(CancellationToken))!;
    }

    private async Task ExecuteAsync(string sql, params object[] parameters)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var batch = new NpgsqlBatch(connection);

        foreach (var statement in sql.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var command = new NpgsqlBatchCommand(statement);

            foreach (var parameter in parameters)
            {
                command.Parameters.Add(new NpgsqlParameter { Value = parameter });
            }

            batch.BatchCommands.Add(command);
        }

        await batch.ExecuteNonQueryAsync(CancellationToken);
    }

    private static NpgsqlCommand Command(NpgsqlConnection connection, string sql, params object[] parameters)
    {
        var command = new NpgsqlCommand(sql, connection);

        foreach (var parameter in parameters)
        {
            command.Parameters.Add(new NpgsqlParameter { Value = parameter });
        }

        return command;
    }

    private sealed record OutboxRow(
        Guid Id,
        int Attempts,
        DateTimeOffset? ProcessedAt,
        DateTimeOffset? FailedAt,
        string? LastError);

    private sealed class FailingHandler : IIntegrationEventHandler<ConversationOpened>
    {
        public const string Error = "Test handler failure";

        public string Name => "test.failing";

        public Task HandleAsync(
            ConversationOpened integrationEvent,
            IntegrationEventContext context,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException(Error);
        }
    }
}
