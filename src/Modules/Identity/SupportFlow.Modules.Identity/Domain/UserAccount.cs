using SupportFlow.BuildingBlocks.Domain;

namespace SupportFlow.Modules.Identity.Domain;

/// <summary>
/// Account of a user authenticated by the external Identity Provider, domain-model §5.
/// </summary>
internal sealed class UserAccount : AggregateRoot
{
    private UserAccount(
        Guid id,
        string externalIssuer,
        string externalSubject,
        AccountType accountType,
        bool isActive)
    {
        Id = id;
        ExternalIssuer = externalIssuer;
        ExternalSubject = externalSubject;
        AccountType = accountType;
        IsActive = isActive;
    }

    public Guid Id { get; }

    /// <summary>
    /// Together with <see cref="ExternalSubject"/> identifies the user at the Identity Provider (ADR-0016).
    /// </summary>
    public string ExternalIssuer { get; }

    public string ExternalSubject { get; }

    public AccountType AccountType { get; }

    public bool IsActive { get; private set; }

    /// <summary>
    /// A customer seen for the first time (JIT, ADR-0016). Staff accounts are never created this way.
    /// </summary>
    public static UserAccount RegisterCustomer(Guid id, string externalIssuer, string externalSubject)
    {
        if (string.IsNullOrWhiteSpace(externalIssuer) || string.IsNullOrWhiteSpace(externalSubject))
        {
            throw new DomainException("External issuer and subject are required.");
        }

        return new UserAccount(id, externalIssuer, externalSubject, AccountType.Customer, isActive: true);
    }
}
