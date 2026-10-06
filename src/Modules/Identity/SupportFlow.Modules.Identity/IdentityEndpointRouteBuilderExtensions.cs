using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace SupportFlow.Modules.Identity;

public static class IdentityEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGroup("/users").WithTags("Identity");
        return endpoints;
    }
}
