using Microsoft.Extensions.DependencyInjection;
using SupportFlow.Modules.Conversations.Application;
using SupportFlow.Modules.Conversations.Domain;
using SupportFlow.Modules.Conversations.Infrastructure;

namespace SupportFlow.Modules.Conversations.IntegrationTests;

/// <summary>
/// Keyset pages of the message history by <c>Seq</c> (components.md §1.3) on a conversation with messages 1..7.
/// </summary>
public sealed class MessagePagingTests(PostgresFixture database) : IAsyncLifetime
{
    private const int MessageCount = 7;

    private readonly ServiceProvider _services = database.CreateServices();
    private Guid _conversationId;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        await using var scope = _services.CreateAsyncScope();
        var opened = await scope.ServiceProvider.GetRequiredService<OpenConversationHandler>().HandleAsync(
            new OpenConversationCommand(Guid.NewGuid(), Guid.NewGuid(), "Payment failed", "Message 1"),
            CancellationToken);
        _conversationId = opened.ConversationId;

        // Test data only: sending messages is not implemented yet, so later messages are inserted directly.
        var context = scope.ServiceProvider.GetRequiredService<ConversationsDbContext>();

        for (var seq = 2; seq <= MessageCount; seq++)
        {
            context.Messages.Add(Message.Create(
                _conversationId,
                seq,
                AuthorType.Agent,
                Guid.NewGuid(),
                MessageBody.Create($"Message {seq}"),
                DateTimeOffset.UtcNow));
        }

        await context.SaveChangesAsync(CancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return _services.DisposeAsync();
    }

    [Fact]
    public async Task ReturnsLatestMessagesAscending()
    {
        var page = await GetPageAsync(afterSeq: null, beforeSeq: null, limit: 3);

        Assert.Equal([5, 6, 7], page.Items.Select(m => m.Seq));
        Assert.True(page.HasMore);
        Assert.Equal("Message 7", page.Items[^1].Body);
    }

    [Fact]
    public async Task PagesToOlderMessages()
    {
        var page = await GetPageAsync(afterSeq: null, beforeSeq: 5, limit: 3);

        Assert.Equal([2, 3, 4], page.Items.Select(m => m.Seq));
        Assert.True(page.HasMore);
    }

    [Fact]
    public async Task ReportsNoOlderMessagesOnFirstPage()
    {
        var page = await GetPageAsync(afterSeq: null, beforeSeq: 3, limit: 3);

        Assert.Equal([1, 2], page.Items.Select(m => m.Seq));
        Assert.False(page.HasMore);
    }

    [Fact]
    public async Task PollsNewerMessages()
    {
        var page = await GetPageAsync(afterSeq: 3, beforeSeq: null, limit: 2);

        Assert.Equal([4, 5], page.Items.Select(m => m.Seq));
        Assert.True(page.HasMore);
    }

    [Fact]
    public async Task ReturnsEmptyPageWhenNothingIsNewer()
    {
        var page = await GetPageAsync(afterSeq: MessageCount, beforeSeq: null, limit: 3);

        Assert.Empty(page.Items);
        Assert.False(page.HasMore);
    }

    private async Task<MessagePage> GetPageAsync(long? afterSeq, long? beforeSeq, int limit)
    {
        await using var scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IMessageQueries>()
            .GetPageAsync(_conversationId, afterSeq, beforeSeq, limit, CancellationToken);
    }
}
