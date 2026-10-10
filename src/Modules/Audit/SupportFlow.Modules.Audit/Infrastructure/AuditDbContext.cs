using Microsoft.EntityFrameworkCore;
using SupportFlow.BuildingBlocks.Persistence;
using SupportFlow.Modules.Audit.Domain;

namespace SupportFlow.Modules.Audit.Infrastructure;

/// <summary>
/// Tables of the <c>audit</c> schema (architecture.md §9), including the module's inbox.
/// </summary>
internal sealed class AuditDbContext(DbContextOptions<AuditDbContext> options) : DbContext(options)
{
    public const string Schema = "audit";

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    public static void Configure(DbContextOptionsBuilder options, string connectionString)
    {
        options
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", Schema))
            .UseSnakeCaseNamingConvention();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfiguration(new AuditEventConfiguration());
        modelBuilder.ApplyInbox();
    }
}
