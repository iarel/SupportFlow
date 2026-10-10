using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SupportFlow.BuildingBlocks.Application;
using SupportFlow.BuildingBlocks.Domain;
using SupportFlow.Modules.Conversations.Application;
using SupportFlow.Modules.Identity.Contracts;

namespace SupportFlow.Modules.Conversations.Endpoints;

/// <summary>
/// <c>POST /conversations</c>, FR-001. Errors shared by all commands (422, 429, 503) are mapped by the API host.
/// </summary>
internal static class OpenConversationEndpoint
{
    public const string IdempotencyKeyHeader = "Idempotency-Key";

    /// <summary>
    /// The request carries the first message (≤ 10 KB) plus metadata, components.md §1.3 [Assumption].
    /// </summary>
    private const long MaxRequestBodySize = 16 * 1024;

    public static RouteHandlerBuilder MapOpenConversation(this IEndpointRouteBuilder conversations)
    {
        // Kestrel stops reading a larger body, including one without Content-Length.
        return conversations.MapPost("/", HandleAsync)
            .RequireAuthorization(IdentityPolicies.Customer)
            .WithMetadata(new RequestSizeLimitAttribute(MaxRequestBodySize));
    }

    private static async Task<IResult> HandleAsync(
        [FromHeader(Name = IdempotencyKeyHeader)] string? idempotencyKey,
        OpenConversationRequest request,
        HttpContext httpContext,
        ICurrentUser currentUser,
        OpenConversationHandler handler,
        IConversationQueries queries,
        CancellationToken cancellationToken)
    {
        // Servers that do not enforce the size limit metadata (such as the test server) still reject a declared
        // larger body.
        if (httpContext.Request.ContentLength > MaxRequestBodySize)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status413PayloadTooLarge);
        }

        // ADR-0014: the key is required and must be a UUID [Assumption].
        if (!Guid.TryParse(idempotencyKey, out var key))
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: $"The {IdempotencyKeyHeader} header is required and must be a UUID.");
        }

        if (request.Subject is null || request.FirstMessage is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Subject and firstMessage are required.");
        }

        OpenConversationResult result;

        try
        {
            result = await handler.HandleAsync(
                new OpenConversationCommand(currentUser.UserId, key, request.Subject, request.FirstMessage),
                cancellationToken);
        }
        catch (DomainException exception)
        {
            // Every rule checked when opening a conversation is about the input, so it is a 400 here rather than
            // the 409 used for state conflicts (components.md §1.3).
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: exception.Message);
        }

        // A repeat returns the current state of the conversation, not the first response (ADR-0014).
        var conversation = await queries.FindAsync(result.ConversationId, cancellationToken);

        if (conversation is null)
        {
            return TypedResults.NotFound();
        }

        // ADR-0007: the version for If-Match of later changes.
        httpContext.Response.Headers.ETag = $"\"{conversation.Version.ToString(CultureInfo.InvariantCulture)}\"";

        var response = ConversationResponse.From(conversation);

        return result.IsCreated
            ? TypedResults.Created($"/conversations/{conversation.Id}", response)
            : TypedResults.Ok(response);
    }
}
