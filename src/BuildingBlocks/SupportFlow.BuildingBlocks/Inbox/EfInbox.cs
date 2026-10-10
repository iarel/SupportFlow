using Microsoft.EntityFrameworkCore;
using SupportFlow.BuildingBlocks.Application;
using SupportFlow.BuildingBlocks.Persistence;

namespace SupportFlow.BuildingBlocks.Inbox;

/// <summary>
/// The module's <c>inbox</c> table (ADR-0008). The context must include
/// <see cref="ModelBuilderExtensions.ApplyInbox"/>.
/// </summary>
public sealed class EfInbox(DbContext context, TimeProvider timeProvider) : IInbox
{
    public async Task<bool> TryRegisterAsync(string handlerName, Guid eventId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(handlerName);

        if (context.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("The inbox must be written inside the unit of work.");
        }

        // A unique violation would abort the transaction, so a duplicate is skipped with ON CONFLICT instead.
        var inserted = await context.Database.ExecuteSqlRawAsync(
            context.Model.InsertIntoInboxSql(),
            [handlerName, eventId, timeProvider.GetUtcNow()],
            cancellationToken);

        return inserted == 1;
    }
}
