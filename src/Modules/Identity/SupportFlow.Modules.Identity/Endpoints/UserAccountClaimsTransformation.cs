using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using SupportFlow.Modules.Identity.Application;

namespace SupportFlow.Modules.Identity.Endpoints;

/// <summary>
/// Maps the token's issuer and subject to the user account, creating a Customer account on the first request
/// (ADR-0016). An inactive account gets no account claims, so authorization policies reject it with 403.
/// </summary>
internal sealed class UserAccountClaimsTransformation(
    ResolveUserAccountHandler resolveUserAccount,
    IHttpContextAccessor httpContextAccessor)
    : IClaimsTransformation
{
    private const string SubjectClaimType = "sub";

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        // The transformation may run more than once per request.
        if (principal.Identity?.IsAuthenticated != true
            || principal.HasClaim(c => c.Type == UserAccountClaimTypes.UserId))
        {
            return principal;
        }

        var subject = principal.FindFirst(SubjectClaimType);

        if (subject is null)
        {
            return principal;
        }

        var account = await resolveUserAccount.HandleAsync(
            subject.Issuer,
            subject.Value,
            httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None);

        if (!account.IsActive)
        {
            return principal;
        }

        var transformed = principal.Clone();
        transformed.AddIdentity(new ClaimsIdentity(
        [
            new Claim(UserAccountClaimTypes.UserId, account.Id.ToString()),
            new Claim(UserAccountClaimTypes.AccountType, account.AccountType.ToString()),
        ]));

        return transformed;
    }
}
