using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace SupportFlow.Modules.Audit;

public static class AuditEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGroup("/audit").WithTags("Audit");
        return endpoints;
    }
}
