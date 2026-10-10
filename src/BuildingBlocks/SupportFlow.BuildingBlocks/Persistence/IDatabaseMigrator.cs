namespace SupportFlow.BuildingBlocks.Persistence;

/// <summary>
/// Applies the migrations of one module's database context. Each module with a database registers one.
/// </summary>
public interface IDatabaseMigrator
{
    Task MigrateAsync(CancellationToken cancellationToken);
}
