using SupportFlow.Modules.Identity.Domain;

namespace SupportFlow.Modules.Identity.Application;

internal interface IUserAccountRepository
{
    Task<UserAccount?> FindByExternalIdAsync(
        string externalIssuer,
        string externalSubject,
        CancellationToken cancellationToken);

    /// <summary>
    /// Saves the account unless one with the same issuer and subject exists. Returns false if a concurrent
    /// request created it first.
    /// </summary>
    Task<bool> TryAddAsync(UserAccount account, CancellationToken cancellationToken);
}
