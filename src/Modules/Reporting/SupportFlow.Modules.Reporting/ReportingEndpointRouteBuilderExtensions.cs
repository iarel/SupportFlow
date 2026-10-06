using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace SupportFlow.Modules.Reporting;

public static class ReportingEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapReportingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGroup("/reports").WithTags("Reporting");
        return endpoints;
    }
}
