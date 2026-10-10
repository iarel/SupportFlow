using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportFlow.Modules.Audit.Domain;

namespace SupportFlow.Modules.Audit.Infrastructure;

internal sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("audit_events");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.SourceEventId);
        builder.Property(e => e.OccurredAt);
        builder.Property(e => e.ActorType).HasConversion<string>().HasMaxLength(16);
        builder.Property(e => e.ActorId);
        builder.Property(e => e.Action);
        builder.Property(e => e.TargetType);
        builder.Property(e => e.TargetId);
        builder.Property(e => e.Details).HasColumnType("jsonb");
        builder.Property(e => e.CorrelationId);
        builder.Ignore(e => e.DomainEvents);

        // One record per integration event (ADR-0008), in addition to the inbox.
        builder.HasIndex(e => e.SourceEventId).IsUnique();

        // Retention of about a year (requirements §4.8).
        builder.HasIndex(e => e.OccurredAt);
    }
}
