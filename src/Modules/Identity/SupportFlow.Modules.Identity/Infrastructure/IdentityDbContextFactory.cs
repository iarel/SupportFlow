using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SupportFlow.Modules.Identity.Infrastructure;

/// <summary>
/// Used only by <c>dotnet ef</c> to create migrations; it does not connect to the database.
/// </summary>
internal sealed class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>();
        IdentityDbContext.Configure(options, "Host=localhost");
        return new IdentityDbContext(options.Options);
    }
}
