using SupportFlow.Modules.Identity.Domain;

namespace SupportFlow.Modules.Identity.Application;

internal sealed class ResolveUserAccountHandler(IUserAccountRepository accounts, TimeProvider timeProvider)
{
    /// <summary>
    /// Finds the account of the external user, creating a Customer account the first time the user is seen
    /// (ADR-0016).
    /// </summary>
    public async Task<UserAccount> HandleAsync(
        string externalIssuer,
        string externalSubject,
        CancellationToken cancellationToken)
    {
        var existing = await accounts.FindByExternalIdAsync(externalIssuer, externalSubject, cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var customer = UserAccount.RegisterCustomer(
            Guid.CreateVersion7(timeProvider.GetUtcNow()),
            externalIssuer,
            externalSubject);

        if (await accounts.TryAddAsync(customer, cancellationToken))
        {
            return customer;
        }

        // A concurrent first request of the same user created the account.
        return await accounts.FindByExternalIdAsync(externalIssuer, externalSubject, cancellationToken)
            ?? throw new InvalidOperationException("User account conflict reported, but the account is not stored.");
    }
}
