using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace SupportFlow.Modules.AIAssistance;

public static class AIAssistanceEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapAIAssistanceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGroup("/conversations/{conversationId:guid}/suggestions").WithTags("AIAssistance");
        return endpoints;
    }
}
