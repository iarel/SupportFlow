using System.Globalization;
using Microsoft.AspNetCore.Http;

namespace SupportFlow.Modules.Conversations.Endpoints;

internal static class HttpResponseExtensions
{
    /// <summary>
    /// ADR-0007: the conversation version that later changes send back in <c>If-Match</c>.
    /// </summary>
    public static void SetConversationVersion(this HttpResponse response, int version)
    {
        response.Headers.ETag = $"\"{version.ToString(CultureInfo.InvariantCulture)}\"";
    }
}
