using System.Globalization;
using Microsoft.AspNetCore.Diagnostics;
using SupportFlow.BuildingBlocks.Application;
using SupportFlow.BuildingBlocks.Domain;

namespace SupportFlow.Api;

/// <summary>
/// Maps application errors shared by all modules to HTTP responses (components.md §1.3). Endpoints map errors
/// whose meaning depends on the command themselves.
/// </summary>
internal sealed class ApplicationExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, retryAfter) = exception switch
        {
            IdempotencyKeyReusedException => (StatusCodes.Status422UnprocessableEntity, (TimeSpan?)null),
            RateLimitExceededException limit => (StatusCodes.Status429TooManyRequests, limit.RetryAfter),

            // The transaction was rolled back; a retry with the same idempotency key is safe (ADR-0014).
            LockTimeoutException => (StatusCodes.Status503ServiceUnavailable, TimeSpan.FromSeconds(1)),
            DomainException => (StatusCodes.Status409Conflict, null),
            _ => (0, null),
        };

        if (statusCode == 0)
        {
            return false;
        }

        httpContext.Response.StatusCode = statusCode;

        if (retryAfter is { } delay)
        {
            var seconds = Math.Max(1, (long)Math.Ceiling(delay.TotalSeconds));
            httpContext.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
        }

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = { Status = statusCode, Title = exception.Message },
        });
    }
}
