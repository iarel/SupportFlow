using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace SupportFlow.Modules.SupportOrganization;

public static class SupportOrganizationEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapSupportOrganizationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGroup("/organization").WithTags("SupportOrganization");
        return endpoints;
    }
}
