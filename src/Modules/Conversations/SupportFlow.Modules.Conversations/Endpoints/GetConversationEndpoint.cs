using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SupportFlow.BuildingBlocks.Application;
using SupportFlow.Modules.Conversations.Application;
using SupportFlow.Modules.Identity.Contracts;

namespace SupportFlow.Modules.Conversations.Endpoints;

/// <summary>
/// <c>GET /conversations/{id}</c>, FR-003. Another customer's conversation is 403 (mapped by the API host).
/// </summary>
internal static class GetConversationEndpoint
{
    public static RouteHandlerBuilder MapGetConversation(this IEndpointRouteBuilder conversations)
    {
        return conversations.MapGet("/{conversationId:guid}", HandleAsync)
            .RequireAuthorization(IdentityPolicies.Customer);
    }

    private static async Task<IResult> HandleAsync(
        Guid conversationId,
        HttpContext httpContext,
        ICurrentUser currentUser,
        GetConversationHandler handler,
        CancellationToken cancellationToken)
    {
        var conversation = await handler.HandleAsync(
            new GetConversationQuery(conversationId, currentUser.UserId),
            cancellationToken);

        if (conversation is null)
        {
            return TypedResults.NotFound();
        }

        httpContext.Response.SetConversationVersion(conversation.Version);
        return TypedResults.Ok(ConversationResponse.From(conversation));
    }
}
