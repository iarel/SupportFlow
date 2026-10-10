using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportFlow.Modules.SupportOrganization.Domain;

namespace SupportFlow.Modules.SupportOrganization.Infrastructure;

internal sealed class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    /// <summary>
    /// The default team, created by the migration: there is no team administration in the MVP.
    /// </summary>
    public static readonly Guid DefaultTeamId = new("0199c6a0-0000-7000-8000-000000000001");

    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("teams");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.Name);
        builder.Property(t => t.IsDefault);
        builder.Ignore(t => t.DomainEvents);

        // "Exactly one default team" spans several aggregates; the index is a storage safety net against a
        // second one.
        builder.HasIndex(t => t.IsDefault).IsUnique().HasFilter("is_default");

        builder.HasData(new Team(DefaultTeamId, "Default", isDefault: true));
    }
}
