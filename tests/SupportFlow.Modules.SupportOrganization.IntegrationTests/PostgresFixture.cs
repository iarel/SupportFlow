using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SupportFlow.Modules.SupportOrganization.Infrastructure;
using SupportFlow.Modules.SupportOrganization.IntegrationTests;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(PostgresFixture))]

namespace SupportFlow.Modules.SupportOrganization.IntegrationTests;

/// <summary>
/// One PostgreSQL container per test run with the module's migrations applied. Requires Docker.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        await using var services = CreateServices();
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SupportOrganizationDbContext>().Database.MigrateAsync();
    }

    public ValueTask DisposeAsync()
    {
        return _container.DisposeAsync();
    }

    /// <summary>
    /// The module as the hosts register it.
    /// </summary>
    public ServiceProvider CreateServices()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SupportFlow"] = _container.GetConnectionString(),
            })
            .Build();

        return new ServiceCollection()
            .AddSupportOrganization(configuration)
            .BuildServiceProvider(validateScopes: true);
    }
}
