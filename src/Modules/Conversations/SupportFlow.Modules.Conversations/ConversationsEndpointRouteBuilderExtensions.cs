using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace SupportFlow.Modules.Conversations;

public static class ConversationsEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapConversationsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGroup("/conversations").WithTags("Conversations");
        return endpoints;
    }
}
