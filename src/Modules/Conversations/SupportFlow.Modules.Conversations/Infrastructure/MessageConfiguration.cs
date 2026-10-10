using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportFlow.Modules.Conversations.Domain;

namespace SupportFlow.Modules.Conversations.Infrastructure;

internal sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("messages");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.ConversationId);
        builder.Property(m => m.Seq);
        builder.Property(m => m.AuthorType).HasConversion<string>().HasMaxLength(16);
        builder.Property(m => m.AuthorId);
        builder.Property(m => m.Body).HasConversion(body => body.Value, value => MessageBody.Create(value));
        builder.Property(m => m.CreatedAt);

        // The conversation and its messages are one unit of retention (domain-model §12).
        builder.HasOne<Conversation>()
            .WithMany()
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Message order (ADR-0005).
        builder.HasIndex(m => new { m.ConversationId, m.Seq }).IsUnique();
    }
}
