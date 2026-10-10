using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SupportFlow.BuildingBlocks.Inbox;
using SupportFlow.BuildingBlocks.Outbox;

namespace SupportFlow.BuildingBlocks.Persistence;

/// <summary>
/// SQL over the outbox and inbox tables of a module. The schema comes from the model; column names are fixed by
/// <see cref="ModelBuilderExtensions"/>. Parameters are positional (<c>{0}</c>, <c>{1}</c>, …).
/// </summary>
internal static class MechanismTableSql
{
    public static string InsertIntoInboxSql(this IModel model)
    {
        return $$"""
            INSERT INTO {{model.TableOf<InboxMessage>()}} (handler_name, event_id, processed_at)
            VALUES ({0}, {1}, {2})
            ON CONFLICT DO NOTHING
            """;
    }

    /// <summary>
    /// Leases a batch of due events (ADR-0003). Parameters: now, lease end, batch size.
    /// </summary>
    public static string ClaimOutboxBatchSql(this IModel model)
    {
        var outbox = model.TableOf<OutboxMessage>();

        return $$"""
            UPDATE {{outbox}}
            SET locked_until = {1}, attempts = attempts + 1
            WHERE id IN (
                SELECT id FROM {{outbox}}
                WHERE processed_at IS NULL
                  AND failed_at IS NULL
                  AND next_attempt_at <= {0}
                  AND (locked_until IS NULL OR locked_until < {0})
                ORDER BY next_attempt_at
                LIMIT {2}
                FOR UPDATE SKIP LOCKED)
            RETURNING *
            """;
    }

    private static string TableOf<TEntity>(this IModel model)
    {
        var entity = model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException($"The context does not include {typeof(TEntity).Name}.");

        return $"\"{entity.GetSchema()}\".\"{entity.GetTableName()}\"";
    }
}
