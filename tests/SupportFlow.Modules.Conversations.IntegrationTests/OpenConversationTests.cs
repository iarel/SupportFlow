using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SupportFlow.BuildingBlocks.Application;
using SupportFlow.BuildingBlocks.Outbox;
using SupportFlow.Modules.Conversations.Application;
using SupportFlow.Modules.Conversations.Domain;
using SupportFlow.Modules.Conversations.Infrastructure;

namespace SupportFlow.Modules.Conversations.IntegrationTests;

/// <summary>
/// The concurrency guarantees of opening a conversation (ADR-0012, ADR-0014) against real PostgreSQL.
/// </summary>
public sealed class OpenConversationTests(PostgresFixture database) : IAsyncDisposable
{
    private const string Subject = "Payment failed";
    private const string FirstMessage = "My card was charged twice.";

    private readonly ServiceProvider _services = database.CreateServices();
    private readonly Guid _customerId = Guid.NewGuid();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public ValueTask DisposeAsync()
    {
        return _services.DisposeAsync();
    }

    [Fact]
    public async Task SavesConversationFirstMessageKeyAndEventsTogether()
    {
        var result = await OpenAsync(Command());

        Assert.True(result.IsCreated);

        await using var scope = _services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ConversationsDbContext>();

        var conversation = await context.Conversations.SingleAsync(c => c.Id == result.ConversationId, CancellationToken);
        Assert.Equal(_customerId, conversation.CustomerId);
        Assert.Equal(PostgresFixture.DefaultTeamId, conversation.TeamId);
        Assert.Equal(ConversationStatus.New, conversation.Status);
        Assert.Equal(1, conversation.LastMessageSeq);

        var message = await context.Messages.SingleAsync(m => m.ConversationId == result.ConversationId, CancellationToken);
        Assert.Equal(1, message.Seq);
        Assert.Equal(AuthorType.Customer, message.AuthorType);
        Assert.Equal(FirstMessage, message.Body.Value);

        Assert.Equal(1, await CountIdempotencyKeysAsync());
        Assert.Equal(
            ["conversations.conversation-opened", "conversations.message-posted"],
            (await OutboxOfAsync(result.ConversationId)).Select(m => m.Type).Order());
    }

    [Fact]
    public async Task RepeatedRequestReturnsExistingConversation()
    {
        var command = Command();
        var first = await OpenAsync(command);

        var repeated = await OpenAsync(command);

        Assert.False(repeated.IsCreated);
        Assert.Equal(first.ConversationId, repeated.ConversationId);
        Assert.Equal(1, await CountConversationsAsync());
        Assert.Equal(2, (await OutboxOfAsync(first.ConversationId)).Count);
    }

    [Fact]
    public async Task RequestWaitsForConcurrentRequestWithSameKeyAndReturnsItsConversation()
    {
        var command = Command();
        var concurrentId = Guid.NewGuid();

        await using var connection = await OpenConnectionAsync();
        await using var concurrent = await connection.BeginTransactionAsync(CancellationToken);
        await InsertIdempotencyKeyAsync(connection, command, concurrentId);

        var request = OpenAsync(command);
        await WaitUntilBlockedOnIdempotencyKeyAsync();
        await concurrent.CommitAsync(CancellationToken);
        var result = await request;

        Assert.False(result.IsCreated);
        Assert.Equal(concurrentId, result.ConversationId);
        Assert.Equal(0, await CountConversationsAsync());
    }

    [Fact]
    public async Task RequestCreatesConversationWhenConcurrentRequestWithSameKeyRollsBack()
    {
        var command = Command();

        await using var connection = await OpenConnectionAsync();
        await using var concurrent = await connection.BeginTransactionAsync(CancellationToken);
        await InsertIdempotencyKeyAsync(connection, command, Guid.NewGuid());

        var request = OpenAsync(command);
        await WaitUntilBlockedOnIdempotencyKeyAsync();
        await concurrent.RollbackAsync(CancellationToken);
        var result = await request;

        Assert.True(result.IsCreated);
        Assert.Equal(1, await CountConversationsAsync());
    }

    [Fact]
    public async Task ParallelRequestsWithSameKeyCreateOneConversation()
    {
        var command = Command();

        var results = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => Task.Run(() => OpenAsync(command))));

        var created = Assert.Single(results, r => r.IsCreated);
        Assert.All(results, r => Assert.Equal(created.ConversationId, r.ConversationId));
        Assert.Equal(1, await CountConversationsAsync());
        Assert.Equal(2, (await OutboxOfAsync(created.ConversationId)).Count);
    }

    [Fact]
    public async Task ParallelRequestsWithSameKeyAndDifferentBodiesCreateOneConversation()
    {
        var idempotencyKey = Guid.NewGuid();

        var outcomes = await Task.WhenAll(
            Enumerable.Range(0, 5).Select(i => TryOpenInParallelAsync(Command(idempotencyKey, $"{FirstMessage} {i}"))));

        Assert.Single(outcomes, e => e is null);
        Assert.All(outcomes.OfType<Exception>(), e => Assert.IsType<IdempotencyKeyReusedException>(e));
        Assert.Equal(1, await CountConversationsAsync());
    }

    [Fact]
    public async Task DailyLimitHoldsForParallelRequests()
    {
        for (var i = 0; i < OpenConversationHandler.DailyConversationLimit - 1; i++)
        {
            await OpenAsync(Command());
        }

        var outcomes = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => TryOpenInParallelAsync(Command())));

        Assert.Single(outcomes, e => e is null);
        Assert.All(outcomes.OfType<Exception>(), e => Assert.IsType<DailyConversationLimitExceededException>(e));
        Assert.Equal(OpenConversationHandler.DailyConversationLimit, await CountConversationsAsync());

        // Rejected requests roll back their keys, so a retry is executed again (ADR-0014).
        Assert.Equal(OpenConversationHandler.DailyConversationLimit, await CountIdempotencyKeysAsync());
    }

    private OpenConversationCommand Command(Guid? idempotencyKey = null, string firstMessage = FirstMessage)
    {
        return new OpenConversationCommand(_customerId, idempotencyKey ?? Guid.NewGuid(), Subject, firstMessage);
    }

    private async Task<OpenConversationResult> OpenAsync(OpenConversationCommand command)
    {
        await using var scope = _services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<OpenConversationHandler>();
        return await handler.HandleAsync(command, CancellationToken);
    }

    /// <summary>
    /// Runs the request on the thread pool and returns its exception, or null if it succeeded.
    /// </summary>
    private Task<Exception?> TryOpenInParallelAsync(OpenConversationCommand command)
    {
        return Task.Run(async () => await Record.ExceptionAsync(() => OpenAsync(command)));
    }

    private async Task<int> CountConversationsAsync()
    {
        await using var scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ConversationsDbContext>()
            .Conversations
            .CountAsync(c => c.CustomerId == _customerId, CancellationToken);
    }

    private async Task<int> CountIdempotencyKeysAsync()
    {
        await using var scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ConversationsDbContext>()
            .Database
            .SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM conversations.idempotency_keys WHERE caller_id = {_customerId}")
            .SingleAsync(CancellationToken);
    }

    private async Task<List<OutboxMessage>> OutboxOfAsync(Guid conversationId)
    {
        await using var scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ConversationsDbContext>()
            .Set<OutboxMessage>()
            .FromSql($"SELECT * FROM conversations.outbox WHERE payload->>'conversationId' = {conversationId.ToString()}")
            .ToListAsync(CancellationToken);
    }

    private async Task<NpgsqlConnection> OpenConnectionAsync()
    {
        var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync(CancellationToken);
        return connection;
    }

    /// <summary>
    /// Acts as a concurrent request that registered the same key and has not committed yet.
    /// </summary>
    private static async Task InsertIdempotencyKeyAsync(
        NpgsqlConnection connection,
        OpenConversationCommand command,
        Guid resourceId)
    {
        await using var insert = new NpgsqlCommand(
            """
            INSERT INTO conversations.idempotency_keys (caller_id, key, operation, request_hash, resource_id, created_at)
            VALUES ($1, $2, $3, $4, $5, now())
            """,
            connection);

        insert.Parameters.Add(new NpgsqlParameter { Value = command.CustomerId });
        insert.Parameters.Add(new NpgsqlParameter { Value = command.IdempotencyKey });
        insert.Parameters.Add(new NpgsqlParameter { Value = OpenConversationHandler.Operation });
        insert.Parameters.Add(new NpgsqlParameter
        {
            Value = RequestHash.Compute(command.Subject, command.FirstMessage).Value.ToArray(),
        });
        insert.Parameters.Add(new NpgsqlParameter { Value = resourceId });

        await insert.ExecuteNonQueryAsync(CancellationToken);
    }

    private async Task WaitUntilBlockedOnIdempotencyKeyAsync()
    {
        await using var connection = await OpenConnectionAsync();
        await using var query = new NpgsqlCommand(
            """
            SELECT count(*) FROM pg_stat_activity
            WHERE wait_event_type = 'Lock' AND query LIKE '%idempotency_keys%'
            """,
            connection);

        for (var attempt = 0; attempt < 200; attempt++)
        {
            if ((long)(await query.ExecuteScalarAsync(CancellationToken))! > 0)
            {
                return;
            }

            await Task.Delay(50, CancellationToken);
        }

        throw new TimeoutException("The request did not block on the idempotency key.");
    }
}
