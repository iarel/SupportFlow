using Microsoft.EntityFrameworkCore;
using SupportFlow.BuildingBlocks.Application;
using SupportFlow.BuildingBlocks.Outbox;

namespace SupportFlow.BuildingBlocks.Persistence;

/// <summary>
/// Tables of BuildingBlocks mechanisms, created in the default schema of the module's context.
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
            builder.Property(m => m.Id).ValueGeneratedNever();
            builder.Property(m => m.Payload).HasColumnType("jsonb");
            builder.HasIndex(m => m.CreatedAt);
        });

        return modelBuilder;
    }
}
