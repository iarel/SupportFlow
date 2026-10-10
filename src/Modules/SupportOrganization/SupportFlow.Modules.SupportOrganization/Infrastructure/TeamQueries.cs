using Microsoft.EntityFrameworkCore;
using SupportFlow.Modules.SupportOrganization.Contracts;

namespace SupportFlow.Modules.SupportOrganization.Infrastructure;

/// <summary>
/// Read side of teams for other modules; reads the table directly, bypassing the aggregate (architecture.md §6).
/// </summary>
internal sealed class TeamQueries(SupportOrganizationDbContext context) : ITeamQueries
{
    public async Task<Guid> GetDefaultTeamIdAsync(CancellationToken cancellationToken)
    {
        var defaultTeamId = await context.Teams
            .Where(t => t.IsDefault)
            .Select(t => (Guid?)t.Id)
            .SingleOrDefaultAsync(cancellationToken);

        return defaultTeamId
            ?? throw new InvalidOperationException("No default team exists; new conversations cannot be routed.");
    }
}
