using Microsoft.EntityFrameworkCore;
using SupportFlow.BuildingBlocks.Persistence;
using SupportFlow.Modules.Conversations.Domain;

namespace SupportFlow.Modules.Conversations.Infrastructure;

/// <summary>
/// Tables of the <c>conversations</c> schema (architecture.md §9), including the module's idempotency keys and
/// outbox.
/// </summary>
internal sealed class ConversationsDbContext(DbContextOptions<ConversationsDbContext> options) : DbContext(options)
{
    public const string Schema = "conversations";

    public DbSet<Conversation> Conversations => Set<Conversation>();

    public DbSet<Message> Messages => Set<Message>();

    public static void Configure(DbContextOptionsBuilder options, string connectionString)
    {
        options
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", Schema))
            .UseSnakeCaseNamingConvention();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfiguration(new ConversationConfiguration());
        modelBuilder.ApplyConfiguration(new MessageConfiguration());
        modelBuilder.ApplyIdempotencyKeys();
        modelBuilder.ApplyOutbox();
    }
}
