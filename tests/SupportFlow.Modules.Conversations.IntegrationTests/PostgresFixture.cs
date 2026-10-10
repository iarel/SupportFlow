using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SupportFlow.Modules.Conversations.Infrastructure;
using SupportFlow.Modules.Conversations.IntegrationTests;
using SupportFlow.Modules.SupportOrganization.Contracts;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(PostgresFixture))]

namespace SupportFlow.Modules.Conversations.IntegrationTests;

/// <summary>
/// One PostgreSQL container per test run with the module's migrations applied. Requires Docker. Tests isolate
/// their data by using new customer ids instead of cleaning tables.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    public static readonly Guid DefaultTeamId = Guid.NewGuid();

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        await using var services = CreateServices();
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ConversationsDbContext>().Database.MigrateAsync();
    }

    public ValueTask DisposeAsync()
    {
        return _container.DisposeAsync();
    }

    /// <summary>
    /// The module as the hosts register it, with SupportOrganization replaced by a fixed default team.
    /// </summary>
    public ServiceProvider CreateServices()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SupportFlow"] = ConnectionString,
            })
            .Build();

        return new ServiceCollection()
            .AddConversations(configuration)
            .AddSingleton<ITeamQueries, DefaultTeam>()
            .BuildServiceProvider(validateScopes: true);
    }

    private sealed class DefaultTeam : ITeamQueries
    {
        public Task<Guid> GetDefaultTeamIdAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(DefaultTeamId);
        }
    }
}
