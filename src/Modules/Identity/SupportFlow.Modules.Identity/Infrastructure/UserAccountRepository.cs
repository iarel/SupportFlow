using Microsoft.EntityFrameworkCore;
using SupportFlow.Modules.Identity.Application;
using SupportFlow.Modules.Identity.Domain;

namespace SupportFlow.Modules.Identity.Infrastructure;

internal sealed class UserAccountRepository(IdentityDbContext context) : IUserAccountRepository
{
    public Task<UserAccount?> FindByExternalIdAsync(
        string externalIssuer,
        string externalSubject,
        CancellationToken cancellationToken)
    {
        return context.UserAccounts
            .AsNoTracking()
            .SingleOrDefaultAsync(
                a => a.ExternalIssuer == externalIssuer && a.ExternalSubject == externalSubject,
                cancellationToken);
    }

    public async Task<bool> TryAddAsync(UserAccount account, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(account);

        var accountType = account.AccountType.ToString();

        var inserted = await context.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO identity.user_accounts (id, external_issuer, external_subject, account_type, is_active)
            VALUES ({account.Id}, {account.ExternalIssuer}, {account.ExternalSubject}, {accountType}, {account.IsActive})
            ON CONFLICT (external_issuer, external_subject) DO NOTHING
            """,
            cancellationToken);

        return inserted == 1;
    }
}
