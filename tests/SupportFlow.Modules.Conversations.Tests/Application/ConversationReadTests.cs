using SupportFlow.BuildingBlocks.Application;
using SupportFlow.Modules.Conversations.Application;
using SupportFlow.Modules.Conversations.Domain;

namespace SupportFlow.Modules.Conversations.Tests.Application;

/// <summary>
/// FR-003, FR-004: a customer reads only their own conversation and its messages.
/// </summary>
public class ConversationReadTests
{
    private static readonly Guid _customerId = Guid.NewGuid();

    private static readonly ConversationSummary _conversation = new(
        Guid.NewGuid(),
        _customerId,
        "Payment failed",
        ConversationStatus.New,
        ConversationPriority.Normal,
        new DateTimeOffset(2026, 10, 7, 12, 0, 0, TimeSpan.Zero),
        Version: 1);

    private readonly FakeQueries _queries = new(_conversation);

    [Fact]
    public async Task ReturnsOwnConversation()
    {
        var conversation = await GetConversationAsync(_conversation.Id, _customerId);

        Assert.Equal(_conversation, conversation);
    }

    [Fact]
    public async Task ReturnsNoConversationWhenItDoesNotExist()
    {
        Assert.Null(await GetConversationAsync(Guid.NewGuid(), _customerId));
    }

    [Fact]
    public async Task DeniesAnotherCustomersConversation()
    {
        await Assert.ThrowsAsync<AccessDeniedException>(() => GetConversationAsync(_conversation.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task ReturnsRequestedMessagePage()
    {
        var page = await GetMessagesAsync(Query(afterSeq: 3, limit: 20));

        Assert.Same(_queries.Page, page);
        Assert.Equal((_conversation.Id, (long?)3, (long?)null, 20), _queries.RequestedPage);
    }

    [Fact]
    public async Task ReturnsNoMessagesWhenConversationDoesNotExist()
    {
        Assert.Null(await GetMessagesAsync(Query() with { ConversationId = Guid.NewGuid() }));
        Assert.Null(_queries.RequestedPage);
    }

    [Fact]
    public async Task DeniesMessagesOfAnotherCustomerWithoutReadingThem()
    {
        await Assert.ThrowsAsync<AccessDeniedException>(
            () => GetMessagesAsync(Query() with { CustomerId = Guid.NewGuid() }));

        Assert.Null(_queries.RequestedPage);
    }

    [Fact]
    public async Task RejectsBothPageBoundaries()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => GetMessagesAsync(Query(afterSeq: 1, beforeSeq: 5)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(GetMessagesHandler.MaxLimit + 1)]
    public async Task RejectsLimitOutOfRange(int limit)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => GetMessagesAsync(Query(limit: limit)));
    }

    private static GetMessagesQuery Query(long? afterSeq = null, long? beforeSeq = null, int limit = 50)
    {
        return new GetMessagesQuery(_conversation.Id, _customerId, afterSeq, beforeSeq, limit);
    }

    private Task<ConversationSummary?> GetConversationAsync(Guid conversationId, Guid customerId)
    {
        return new GetConversationHandler(_queries).HandleAsync(
            new GetConversationQuery(conversationId, customerId),
            TestContext.Current.CancellationToken);
    }

    private Task<MessagePage?> GetMessagesAsync(GetMessagesQuery query)
    {
        return new GetMessagesHandler(_queries, _queries).HandleAsync(query, TestContext.Current.CancellationToken);
    }

    private sealed class FakeQueries(ConversationSummary conversation) : IConversationQueries, IMessageQueries
    {
        public MessagePage Page { get; } = new([], HasMore: false);

        public (Guid ConversationId, long? AfterSeq, long? BeforeSeq, int Limit)? RequestedPage { get; private set; }

        public Task<ConversationSummary?> FindAsync(Guid conversationId, CancellationToken cancellationToken)
        {
            return Task.FromResult(conversationId == conversation.Id ? conversation : null);
        }

        public Task<MessagePage> GetPageAsync(
            Guid conversationId,
            long? afterSeq,
            long? beforeSeq,
            int limit,
            CancellationToken cancellationToken)
        {
            RequestedPage = (conversationId, afterSeq, beforeSeq, limit);
            return Task.FromResult(Page);
        }
    }
}
