using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportFlow.Modules.Identity.Domain;

namespace SupportFlow.Modules.Identity.Infrastructure;

internal sealed class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        builder.ToTable("user_accounts");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.ExternalIssuer);
        builder.Property(a => a.ExternalSubject);
        builder.Property(a => a.AccountType).HasConversion<string>().HasMaxLength(16);
        builder.Property(a => a.IsActive);
        builder.Ignore(a => a.DomainEvents);

        // One account per external user (ADR-0016).
        builder.HasIndex(a => new { a.ExternalIssuer, a.ExternalSubject }).IsUnique();
    }
}
