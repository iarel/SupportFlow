using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SupportFlow.BuildingBlocks.Outbox;
using SupportFlow.BuildingBlocks.Persistence;
using SupportFlow.Modules.Audit;
using SupportFlow.Modules.Conversations;
using SupportFlow.Modules.SupportOrganization;
using SupportFlow.Worker.IntegrationTests;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(WorkerFixture))]


namespace SupportFlow.Worker.IntegrationTests;

/// <summary>
/// PostgreSQL container with the migrations of the modules that publish and consume events in these tests.
/// Requires Docker.
/// </summary>
public sealed class WorkerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        await using var services = CreateServices(new OutboxDispatcherOptions());
        await services.MigrateDatabasesAsync(CancellationToken.None);
    }

    public ValueTask DisposeAsync()
    {
        return _container.DisposeAsync();
    }

    /// <summary>
    /// The modules as the Worker registers them, with the given delivery settings.
    /// </summary>
    public ServiceProvider CreateServices(
        OutboxDispatcherOptions options,
        Action<IServiceCollection>? configure = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SupportFlow"] = _container.GetConnectionString(),
            })
            .Build();

        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton(options)
            .AddConversations(configuration)
            .AddSupportOrganization(configuration)
            .AddAudit(configuration);

        configure?.Invoke(services);

        return services.BuildServiceProvider(validateScopes: true);
    }

    public async Task<NpgsqlConnection> OpenConnectionAsync()
    {
        var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        return connection;
    }
}
