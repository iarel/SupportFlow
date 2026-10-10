using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SupportFlow.Modules.SupportOrganization.Infrastructure;

/// <summary>
/// Used only by <c>dotnet ef</c> to create migrations; it does not connect to the database.
/// </summary>
internal sealed class SupportOrganizationDbContextFactory : IDesignTimeDbContextFactory<SupportOrganizationDbContext>
{
    public SupportOrganizationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SupportOrganizationDbContext>();
        SupportOrganizationDbContext.Configure(options, "Host=localhost");
        return new SupportOrganizationDbContext(options.Options);
    }
}
