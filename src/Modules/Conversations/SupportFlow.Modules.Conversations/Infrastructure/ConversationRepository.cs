using Microsoft.EntityFrameworkCore;
using SupportFlow.Modules.Conversations.Application;
using SupportFlow.Modules.Conversations.Domain;

namespace SupportFlow.Modules.Conversations.Infrastructure;

internal sealed class ConversationRepository(ConversationsDbContext context) : IConversationRepository
{
    // Advisory locks share one key space in the database; the prefix keeps this lock apart from others.
    private const string CreationLockPrefix = "conversations.open:";

    public async Task LockCustomerConversationCreationAsync(Guid customerId, CancellationToken cancellationToken)
    {
        if (context.Database.CurrentTransaction is null)
        {
            // Outside a transaction the lock would be released at once.
            throw new InvalidOperationException("The customer lock must be taken inside the unit of work.");
        }

        var lockKey = CreationLockPrefix + customerId.ToString("N");

        await context.Database.ExecuteSqlAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))",
            cancellationToken);
    }

    public async Task<OpenedConversations> CountOpenedByCustomerSinceAsync(
        Guid customerId,
        DateTimeOffset since,
        CancellationToken cancellationToken)
    {
        var opened = await context.Conversations
            .Where(c => c.CustomerId == customerId && c.CreatedAt > since)
            .GroupBy(_ => 1)
            .Select(g => new OpenedConversations(g.Count(), g.Min(c => (DateTimeOffset?)c.CreatedAt)))
            .SingleOrDefaultAsync(cancellationToken);

        return opened ?? OpenedConversations.None;
    }

    public void Add(Conversation conversation)
    {
        context.Conversations.Add(conversation);
    }
}
