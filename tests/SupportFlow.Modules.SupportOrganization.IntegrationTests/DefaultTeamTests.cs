using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SupportFlow.Modules.SupportOrganization.Contracts;
using SupportFlow.Modules.SupportOrganization.Domain;
using SupportFlow.Modules.SupportOrganization.Infrastructure;

namespace SupportFlow.Modules.SupportOrganization.IntegrationTests;

/// <summary>
/// Routing of new conversations to the default team (domain-model §4).
/// </summary>
public sealed class DefaultTeamTests(PostgresFixture database) : IAsyncDisposable
{
    private readonly ServiceProvider _services = database.CreateServices();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public ValueTask DisposeAsync()
    {
        return _services.DisposeAsync();
    }

    [Fact]
    public async Task ReturnsTeamSeededByMigration()
    {
        await using var scope = _services.CreateAsyncScope();

        var teamId = await scope.ServiceProvider.GetRequiredService<ITeamQueries>()
            .GetDefaultTeamIdAsync(CancellationToken);

        Assert.Equal(TeamConfiguration.DefaultTeamId, teamId);
    }

    [Fact]
    public async Task RejectsSecondDefaultTeam()
    {
        await using var scope = _services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<SupportOrganizationDbContext>();

        context.Teams.Add(new Team(Guid.NewGuid(), "Second", isDefault: true));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync(CancellationToken));
        Assert.Equal(
            PostgresErrorCodes.UniqueViolation,
            Assert.IsType<PostgresException>(exception.InnerException).SqlState);
    }
}
