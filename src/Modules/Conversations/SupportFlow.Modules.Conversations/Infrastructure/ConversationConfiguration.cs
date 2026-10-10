using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportFlow.Modules.Conversations.Domain;

namespace SupportFlow.Modules.Conversations.Infrastructure;

internal sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("conversations");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.CustomerId);
        builder.Property(c => c.TeamId);

        // The length limit is a domain rule and an [Assumption]; the column does not repeat it.
        builder.Property(c => c.Subject);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(c => c.Priority).HasConversion<string>().HasMaxLength(16);
        builder.Property(c => c.CreatedAt);
        builder.Ignore(c => c.DomainEvents);

        // Daily conversation limit (ADR-0012).
        builder.HasIndex(c => new { c.CustomerId, c.CreatedAt });
    }
}
