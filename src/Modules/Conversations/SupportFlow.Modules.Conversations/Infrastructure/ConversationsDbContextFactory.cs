using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SupportFlow.Modules.Conversations.Infrastructure;

/// <summary>
/// Used only by <c>dotnet ef</c> to create migrations; it does not connect to the database.
/// </summary>
internal sealed class ConversationsDbContextFactory : IDesignTimeDbContextFactory<ConversationsDbContext>
{
    public ConversationsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ConversationsDbContext>();
        ConversationsDbContext.Configure(options, "Host=localhost");
        return new ConversationsDbContext(options.Options);
    }
}
