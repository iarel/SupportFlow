using Microsoft.EntityFrameworkCore;
using SupportFlow.BuildingBlocks.Application;
using SupportFlow.BuildingBlocks.Inbox;
using SupportFlow.BuildingBlocks.Outbox;

namespace SupportFlow.BuildingBlocks.Persistence;

/// <summary>
/// Tables of BuildingBlocks mechanisms, created in the default schema of the module's context. Outbox and inbox
/// columns are named explicitly because the dispatcher and the inbox use them in SQL.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// <c>idempotency_keys</c>, ADR-0014.
    /// </summary>
    public static ModelBuilder ApplyIdempotencyKeys(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<IdempotencyKey>(builder =>
        {
            builder.ToTable("idempotency_keys");
            builder.HasKey(k => new { k.CallerId, k.Key, k.Operation });
            builder.Property(k => k.RequestHash).HasMaxLength(RequestHash.SizeInBytes);
            builder.HasIndex(k => k.CreatedAt);
        });

        return modelBuilder;
    }

    /// <summary>
    /// <c>outbox</c>, ADR-0003.
    /// </summary>
    public static ModelBuilder ApplyOutbox(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.ToTable("outbox");
            builder.HasKey(m => m.Id);
            builder.Property(m => m.Id).HasColumnName("id").ValueGeneratedNever();
            builder.Property(m => m.Type).HasColumnName("type");
            builder.Property(m => m.Payload).HasColumnName("payload").HasColumnType("jsonb");
            builder.Property(m => m.CreatedAt).HasColumnName("created_at");
            builder.Property(m => m.Attempts).HasColumnName("attempts");
            builder.Property(m => m.NextAttemptAt).HasColumnName("next_attempt_at");
            builder.Property(m => m.LockedUntil).HasColumnName("locked_until");
            builder.Property(m => m.ProcessedAt).HasColumnName("processed_at");
            builder.Property(m => m.FailedAt).HasColumnName("failed_at");
            builder.Property(m => m.LastError).HasColumnName("last_error");

            // Pending events, in the order the dispatcher claims them.
            builder.HasIndex(m => m.NextAttemptAt).HasFilter("processed_at IS NULL AND failed_at IS NULL");

            // Cleanup of delivered events.
            builder.HasIndex(m => m.ProcessedAt);
        });

        return modelBuilder;
    }

    /// <summary>
    /// <c>inbox</c>, ADR-0008.
    /// </summary>
    public static ModelBuilder ApplyInbox(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<InboxMessage>(builder =>
        {
            builder.ToTable("inbox");
            builder.HasKey(m => new { m.HandlerName, m.EventId });
            builder.Property(m => m.HandlerName).HasColumnName("handler_name");
            builder.Property(m => m.EventId).HasColumnName("event_id");
            builder.Property(m => m.ProcessedAt).HasColumnName("processed_at");
            builder.HasIndex(m => m.ProcessedAt);
        });

        return modelBuilder;
    }
}
