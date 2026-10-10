using SupportFlow.BuildingBlocks.Application;
using SupportFlow.BuildingBlocks.Domain;
using SupportFlow.Modules.Conversations.Application;
using SupportFlow.Modules.Conversations.Contracts;
using SupportFlow.Modules.SupportOrganization.Contracts;

namespace SupportFlow.Modules.Conversations.Tests.Application;

public class OpenConversationHandlerTests
{
    private const string Subject = "Payment failed";
    private const string FirstMessage = "My card was charged twice.";

    private static readonly Guid _customerId = Guid.NewGuid();
    private static readonly Guid _idempotencyKey = Guid.NewGuid();
    private static readonly Guid _defaultTeamId = Guid.NewGuid();
    private static readonly DateTimeOffset _now = new(2026, 10, 7, 12, 0, 0, TimeSpan.Zero);

    private static readonly IdempotencyScope _scope =
        new(OpenConversationHandler.Operation, _customerId, _idempotencyKey);

    private readonly InMemoryConversationStore _store = new();

    [Fact]
    public async Task OpensConversationWithFirstMessageInDefaultTeam()
    {
        var result = await HandleAsync(Command());

        Assert.True(result.IsCreated);
        var conversation = Assert.Single(_store.Conversations);
        var message = Assert.Single(_store.Messages);
        Assert.Equal(result.ConversationId, conversation.Id);
        Assert.Equal(_defaultTeamId, conversation.TeamId);
        Assert.Equal(conversation.Id, message.ConversationId);
    }

    [Fact]
    public async Task PublishesConversationOpenedAndFirstMessagePostedInSameTransaction()
    {
        var result = await HandleAsync(Command());

        var message = Assert.Single(_store.Messages);
        Assert.Collection(
            _store.Outbox,
            e => Assert.Equal(new ConversationOpened(result.ConversationId, _customerId, _defaultTeamId, _now), e),
            e => Assert.Equal(
                new MessagePosted(result.ConversationId, message.Id, 1, MessageAuthorType.Customer, _customerId, _now),
                e));
        Assert.Empty(Assert.Single(_store.Conversations).DomainEvents);
    }

    [Fact]
    public async Task DoesNotPublishEventsForRepeatedRequest()
    {
        await HandleAsync(Command());
        _store.Outbox.Clear();

        await HandleAsync(Command());

        Assert.Empty(_store.Outbox);
    }

    [Fact]
    public async Task StoresIdempotencyKeyWithCreatedConversation()
    {
        var result = await HandleAsync(Command());

        var record = Assert.Single(_store.IdempotencyKeys);
        Assert.Equal(_scope, record.Key);
        Assert.Equal(result.ConversationId, record.Value.ResourceId);
        Assert.Equal(RequestHash.Compute(Subject, FirstMessage), record.Value.RequestHash);
    }

    [Fact]
    public async Task RegistersIdempotencyKeyThenLocksCustomerThenChecksDailyLimit()
    {
        await HandleAsync(Command());

        Assert.Equal(
            [
                nameof(IIdempotencyStore.RegisterAsync),
                nameof(IConversationRepository.LockCustomerConversationCreationAsync),
                nameof(IConversationRepository.CountOpenedByCustomerSinceAsync),
            ],
            _store.TransactionLog);
    }

    [Fact]
    public async Task ReturnsExistingConversationForRepeatedRequestWithoutCheckingLimit()
    {
        var first = await HandleAsync(Command());
        _store.TransactionLog.Clear();
        _store.OpenedSinceCount = OpenConversationHandler.DailyConversationLimit;

        var repeated = await HandleAsync(Command());

        Assert.False(repeated.IsCreated);
        Assert.Equal(first.ConversationId, repeated.ConversationId);
        Assert.Single(_store.Conversations);
        Assert.Empty(_store.TransactionLog);
    }

    [Fact]
    public async Task RejectsRepeatedKeyWithDifferentRequest()
    {
        await HandleAsync(Command());

        await Assert.ThrowsAsync<IdempotencyKeyReusedException>(
            () => HandleAsync(Command(firstMessage: "Another message")));

        Assert.Single(_store.Conversations);
    }

    [Fact]
    public async Task HashesRequestAsReceivedBeforeNormalization()
    {
        await HandleAsync(Command());

        await Assert.ThrowsAsync<IdempotencyKeyReusedException>(
            () => HandleAsync(Command(subject: $"  {Subject}  ")));
    }

    [Fact]
    public async Task AllowsSameKeyForAnotherCustomer()
    {
        await HandleAsync(Command());

        var result = await HandleAsync(Command(customerId: Guid.NewGuid()));

        Assert.True(result.IsCreated);
        Assert.Equal(2, _store.Conversations.Count);
    }

    [Fact]
    public async Task RejectsWhenDailyLimitIsReachedAndDoesNotKeepKey()
    {
        _store.OpenedSinceCount = OpenConversationHandler.DailyConversationLimit;

        await Assert.ThrowsAsync<DailyConversationLimitExceededException>(() => HandleAsync(Command()));

        Assert.Empty(_store.Conversations);
        Assert.Empty(_store.Messages);
        Assert.Empty(_store.IdempotencyKeys);
        Assert.Empty(_store.Outbox);
    }

    [Fact]
    public async Task SuggestsRetryWhenEarliestConversationLeavesWindow()
    {
        _store.OpenedSinceCount = OpenConversationHandler.DailyConversationLimit;
        _store.EarliestOpenedAt = _now.AddHours(-20);

        var exception = await Assert.ThrowsAsync<DailyConversationLimitExceededException>(() => HandleAsync(Command()));

        Assert.Equal(TimeSpan.FromHours(4), exception.RetryAfter);
    }

    [Fact]
    public async Task RetryAfterRejectionExecutesCommandAgain()
    {
        _store.OpenedSinceCount = OpenConversationHandler.DailyConversationLimit;
        await Assert.ThrowsAsync<DailyConversationLimitExceededException>(() => HandleAsync(Command()));
        _store.OpenedSinceCount = 0;

        var result = await HandleAsync(Command());

        Assert.True(result.IsCreated);
    }

    [Fact]
    public async Task CreatesWhenBelowDailyLimit()
    {
        _store.OpenedSinceCount = OpenConversationHandler.DailyConversationLimit - 1;

        var result = await HandleAsync(Command());

        Assert.True(result.IsCreated);
    }

    [Fact]
    public async Task CountsConversationsOfLast24Hours()
    {
        await HandleAsync(Command());

        Assert.Equal(_now.AddDays(-1), _store.CountedSince);
    }

    [Fact]
    public async Task ReturnsConcurrentlyCreatedConversationWhenSameRequestCommitsFirst()
    {
        var winnerId = Guid.NewGuid();
        _store.ConcurrentWinner = (_scope, new IdempotencyRecord(RequestHash.Compute(Subject, FirstMessage), winnerId));
        _store.OpenedSinceCount = OpenConversationHandler.DailyConversationLimit;

        var result = await HandleAsync(Command());

        Assert.False(result.IsCreated);
        Assert.Equal(winnerId, result.ConversationId);
        Assert.Empty(_store.Conversations);
        Assert.Empty(_store.Messages);
    }

    [Fact]
    public async Task RejectsConcurrentRequestWithSameKeyAndDifferentBody()
    {
        _store.ConcurrentWinner = (_scope, new IdempotencyRecord(RequestHash.Compute(Subject, "Other"), Guid.NewGuid()));

        await Assert.ThrowsAsync<IdempotencyKeyReusedException>(() => HandleAsync(Command()));

        Assert.Empty(_store.Conversations);
    }

    [Fact]
    public async Task RejectsInvalidFirstMessageWithoutOpeningTransaction()
    {
        await Assert.ThrowsAsync<DomainException>(() => HandleAsync(Command(firstMessage: " ")));

        Assert.Empty(_store.Conversations);
        Assert.Empty(_store.IdempotencyKeys);
        Assert.Empty(_store.TransactionLog);
    }

    private static OpenConversationCommand Command(
        Guid? customerId = null,
        string subject = Subject,
        string firstMessage = FirstMessage)
    {
        return new OpenConversationCommand(customerId ?? _customerId, _idempotencyKey, subject, firstMessage);
    }

    private Task<OpenConversationResult> HandleAsync(OpenConversationCommand command)
    {
        var handler = new OpenConversationHandler(
            _store,
            _store,
            _store,
            _store,
            new DefaultTeam(_defaultTeamId),
            _store,
            new FixedTimeProvider(_now));

        return handler.HandleAsync(command, TestContext.Current.CancellationToken);
    }

    private sealed class DefaultTeam(Guid teamId) : ITeamQueries
    {
        public Task<Guid> GetDefaultTeamIdAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(teamId);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return now;
        }
    }
}
