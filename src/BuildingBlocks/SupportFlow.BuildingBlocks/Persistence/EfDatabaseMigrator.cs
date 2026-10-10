using Microsoft.EntityFrameworkCore;

namespace SupportFlow.BuildingBlocks.Persistence;

public sealed class EfDatabaseMigrator<TContext>(TContext context) : IDatabaseMigrator
    where TContext : DbContext
{
    public Task MigrateAsync(CancellationToken cancellationToken)
    {
        return context.Database.MigrateAsync(cancellationToken);
    }
}
