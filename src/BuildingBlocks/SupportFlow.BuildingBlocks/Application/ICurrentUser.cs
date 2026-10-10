namespace SupportFlow.BuildingBlocks.Application;

/// <summary>
/// The authenticated user of the current request (ADR-0011, ADR-0016). Implemented by the Identity module.
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// <c>UserAccount.Id</c>; for a customer it is the <c>CustomerId</c>. Throws when the request has no active
    /// user account; endpoints that use it require authorization.
    /// </summary>
    Guid UserId { get; }
}
