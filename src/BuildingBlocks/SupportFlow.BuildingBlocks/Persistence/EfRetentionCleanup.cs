using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SupportFlow.BuildingBlocks.Inbox;
using SupportFlow.BuildingBlocks.Outbox;

namespace SupportFlow.BuildingBlocks.Persistence;

/// <summary>
/// Deletes, in batches, the expired rows of whichever mechanism tables the module's context has: processed outbox
/// events, inbox records and idempotency keys (ADR-0003, ADR-0014).
/// </summary>
public sealed class EfRetentionCleanup<TContext>(
    IServiceScopeFactory scopeFactory,
    RetentionOptions options,
    TimeProvider timeProvider)
    : IRetentionCleanup
    where TContext : DbContext
{
    public async Task<int> CleanUpAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        var now = timeProvider.GetUtcNow();
        var deliveredBefore = now - options.DeliveredEvents;
        var createdBefore = now - options.IdempotencyKeys;
        var deleted = 0;

        if (Has<OutboxMessage>(context))
        {
            deleted += await DeleteInBatchesAsync(
                context.Set<OutboxMessage>().Where(m => m.ProcessedAt < deliveredBefore).OrderBy(m => m.ProcessedAt),
                cancellationToken);
        }

        if (Has<InboxMessage>(context))
        {
            deleted += await DeleteInBatchesAsync(
                context.Set<InboxMessage>().Where(m => m.ProcessedAt < deliveredBefore).OrderBy(m => m.ProcessedAt),
                cancellationToken);
        }

        if (Has<IdempotencyKey>(context))
        {
            deleted += await DeleteInBatchesAsync(
                context.Set<IdempotencyKey>().Where(k => k.CreatedAt < createdBefore).OrderBy(k => k.CreatedAt),
                cancellationToken);
        }

        return deleted;
    }

    private static bool Has<TEntity>(DbContext context)
    {
        return context.Model.FindEntityType(typeof(TEntity)) is not null;
    }

    private async Task<int> DeleteInBatchesAsync<TEntity>(
        IQueryable<TEntity> expired,
        CancellationToken cancellationToken)
    {
        var total = 0;
        int deleted;

        do
        {
            deleted = await expired.Take(options.BatchSize).ExecuteDeleteAsync(cancellationToken);
            total += deleted;
        }
        while (deleted == options.BatchSize);

        return total;
    }
}
