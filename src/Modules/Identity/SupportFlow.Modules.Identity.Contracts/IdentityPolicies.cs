namespace SupportFlow.Modules.Identity.Contracts;

/// <summary>
/// Authorization policies registered by the Identity module, for other modules' endpoints.
/// </summary>
public static class IdentityPolicies
{
    /// <summary>
    /// An active user account of type Customer (ADR-0016).
    /// </summary>
    public const string Customer = "Customer";
}
