using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SupportFlow.BuildingBlocks.Application;
using SupportFlow.Modules.Conversations.Application;
using SupportFlow.Modules.Identity.Contracts;

namespace SupportFlow.Modules.Conversations.Endpoints;

/// <summary>
/// <c>GET /conversations/{id}/messages</c>, FR-004. Without <c>afterSeq</c> and <c>beforeSeq</c> returns the
/// latest messages; <c>beforeSeq</c> pages to older ones, <c>afterSeq</c> polls for newer ones (components.md §1.3).
/// </summary>
internal static class GetMessagesEndpoint
{
    public static RouteHandlerBuilder MapGetMessages(this IEndpointRouteBuilder conversations)
    {
        return conversations.MapGet("/{conversationId:guid}/messages", HandleAsync)
            .RequireAuthorization(IdentityPolicies.Customer);
    }

    private static async Task<IResult> HandleAsync(
        Guid conversationId,
        [FromQuery] long? afterSeq,
        [FromQuery] long? beforeSeq,
        [FromQuery] int? limit,
        ICurrentUser currentUser,
        GetMessagesHandler handler,
        CancellationToken cancellationToken)
    {
        if (afterSeq is not null && beforeSeq is not null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Only one of afterSeq and beforeSeq can be set.");
        }

        if (limit is < 1 or > GetMessagesHandler.MaxLimit)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: $"limit must be between 1 and {GetMessagesHandler.MaxLimit}.");
        }

        var page = await handler.HandleAsync(
            new GetMessagesQuery(
                conversationId,
                currentUser.UserId,
                afterSeq,
                beforeSeq,
                limit ?? GetMessagesHandler.DefaultLimit),
            cancellationToken);

        return page is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(MessagePageResponse.From(page));
    }
}
