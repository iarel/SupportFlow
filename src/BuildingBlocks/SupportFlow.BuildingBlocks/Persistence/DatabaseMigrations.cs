using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace SupportFlow.BuildingBlocks.Persistence;

public static class DatabaseMigrations
{
    public static IServiceCollection AddDatabaseMigrator<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        return services.AddScoped<IDatabaseMigrator, EfDatabaseMigrator<TContext>>();
    }

    /// <summary>
    /// Applies the migrations of all modules: on API startup in Development, otherwise by the <c>migrate</c> deployment
    /// step (ADR-0017).
    /// </summary>
    public static async Task MigrateDatabasesAsync(this IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();

        foreach (var migrator in scope.ServiceProvider.GetServices<IDatabaseMigrator>())
        {
            await migrator.MigrateAsync(cancellationToken);
        }
    }
}
