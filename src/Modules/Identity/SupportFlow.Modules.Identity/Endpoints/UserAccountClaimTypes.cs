namespace SupportFlow.Modules.Identity.Endpoints;

/// <summary>
/// Claims added to an authenticated request with an active user account (ADR-0016).
/// </summary>
internal static class UserAccountClaimTypes
{
    public const string UserId = "supportflow:user_id";

    public const string AccountType = "supportflow:account_type";
}
