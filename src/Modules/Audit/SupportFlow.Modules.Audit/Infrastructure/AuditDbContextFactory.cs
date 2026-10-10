using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SupportFlow.Modules.Audit.Infrastructure;

/// <summary>
/// Used only by <c>dotnet ef</c> to create migrations; it does not connect to the database.
/// </summary>
internal sealed class AuditDbContextFactory : IDesignTimeDbContextFactory<AuditDbContext>
{
    public AuditDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AuditDbContext>();
        AuditDbContext.Configure(options, "Host=localhost");
        return new AuditDbContext(options.Options);
    }
}
