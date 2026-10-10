using Microsoft.EntityFrameworkCore;
using SupportFlow.Modules.Conversations.Application;

namespace SupportFlow.Modules.Conversations.Infrastructure;

/// <summary>
/// Keyset pages over the unique <c>(conversation_id, seq)</c> index. One extra row is read to tell whether more
/// messages exist in the requested direction.
/// </summary>
internal sealed class MessageQueries(ConversationsDbContext context) : IMessageQueries
{
    public async Task<MessagePage> GetPageAsync(
        Guid conversationId,
        long? afterSeq,
        long? beforeSeq,
        int limit,
        CancellationToken cancellationToken)
    {
        var messages = context.Messages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId);

        if (afterSeq is { } after)
        {
            var newer = await messages
                .Where(m => m.Seq > after)
                .OrderBy(m => m.Seq)
                .Take(limit + 1)
                .Select(m => new MessageView(m.Id, m.Seq, m.AuthorType, m.Body.Value, m.CreatedAt))
                .ToListAsync(cancellationToken);

            return new MessagePage(newer.Take(limit).ToList(), newer.Count > limit);
        }

        if (beforeSeq is { } before)
        {
            messages = messages.Where(m => m.Seq < before);
        }

        // The latest messages before the boundary, read newest first and returned ascending.
        var older = await messages
            .OrderByDescending(m => m.Seq)
            .Take(limit + 1)
            .Select(m => new MessageView(m.Id, m.Seq, m.AuthorType, m.Body.Value, m.CreatedAt))
            .ToListAsync(cancellationToken);

        return new MessagePage(older.Take(limit).Reverse().ToList(), older.Count > limit);
    }
}
