using Microsoft.AspNetCore.Http;
using SupportFlow.BuildingBlocks.Application;

namespace SupportFlow.Modules.Identity.Endpoints;

/// <summary>
/// The current user from the claims added by <see cref="UserAccountClaimsTransformation"/>.
/// </summary>
internal sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid UserId =>
        Guid.TryParse(httpContextAccessor.HttpContext?.User.FindFirst(UserAccountClaimTypes.UserId)?.Value, out var userId)
            ? userId
            : throw new InvalidOperationException("The request has no active user account.");
}
