using Microsoft.EntityFrameworkCore;
using SupportFlow.Modules.Identity.Domain;

namespace SupportFlow.Modules.Identity.Infrastructure;

/// <summary>
/// Tables of the <c>identity</c> schema (architecture.md §9).
/// </summary>
internal sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    public const string Schema = "identity";

    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();

    public static void Configure(DbContextOptionsBuilder options, string connectionString)
    {
        options
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", Schema))
            .UseSnakeCaseNamingConvention();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfiguration(new UserAccountConfiguration());
    }
}
