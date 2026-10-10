using Microsoft.EntityFrameworkCore;
using SupportFlow.Modules.SupportOrganization.Domain;

namespace SupportFlow.Modules.SupportOrganization.Infrastructure;

/// <summary>
/// Tables of the <c>organization</c> schema (architecture.md §9).
/// </summary>
internal sealed class SupportOrganizationDbContext(DbContextOptions<SupportOrganizationDbContext> options)
    : DbContext(options)
{
    public const string Schema = "organization";

    public DbSet<Team> Teams => Set<Team>();

    public static void Configure(DbContextOptionsBuilder options, string connectionString)
    {
        options
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", Schema))
            .UseSnakeCaseNamingConvention();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfiguration(new TeamConfiguration());
    }
}
