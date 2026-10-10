using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SupportFlow.Modules.Conversations.Endpoints;

namespace SupportFlow.Modules.Conversations;

public static class ConversationsEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapConversationsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var conversations = endpoints.MapGroup("/conversations").WithTags("Conversations");
        conversations.MapOpenConversation();
        conversations.MapGetConversation();
        conversations.MapGetMessages();
        return endpoints;
    }
}
