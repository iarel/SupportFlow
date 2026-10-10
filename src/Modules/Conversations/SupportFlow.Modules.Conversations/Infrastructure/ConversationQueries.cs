using Microsoft.EntityFrameworkCore;
using SupportFlow.Modules.Conversations.Application;

namespace SupportFlow.Modules.Conversations.Infrastructure;

internal sealed class ConversationQueries(ConversationsDbContext context) : IConversationQueries
{
    public Task<ConversationSummary?> FindAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        return context.Conversations
            .AsNoTracking()
            .Where(c => c.Id == conversationId)
            .Select(c => new ConversationSummary(
                c.Id,
                c.CustomerId,
                c.Subject,
                c.Status,
                c.Priority,
                c.CreatedAt,
                c.Version))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
