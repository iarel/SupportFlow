using System.Net;
using System.Net.Http.Json;

namespace SupportFlow.Api.IntegrationTests;

/// <summary>
/// <c>GET /conversations/{id}</c> (FR-003) and <c>GET /conversations/{id}/messages</c> (FR-004): a customer reads
/// only their own conversation (components.md §1.3).
/// </summary>
public sealed class ReadConversationEndpointTests(ApiFactory api)
{
    private const string Subject = "Payment failed";
    private const string FirstMessage = "My card was charged twice.";

    private readonly string _externalSubject = Guid.NewGuid().ToString();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task LocationOfOpenedConversationReturnsItWithETag()
    {
        using var client = api.CreateClientFor(_externalSubject);
        using var opened = await OpenAsync(client);

        using var response = await client.GetAsync(opened.Headers.Location, CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("\"1\"", response.Headers.ETag?.Tag);
        var conversation = await response.Content.ReadFromJsonAsync<ConversationDto>(CancellationToken);
        Assert.Equal(Subject, conversation?.Subject);
        Assert.Equal("New", conversation?.Status);
    }

    [Fact]
    public async Task AnotherCustomersConversationReturns403()
    {
        using var owner = api.CreateClientFor(_externalSubject);
        using var opened = await OpenAsync(owner);
        using var stranger = api.CreateClientFor(Guid.NewGuid().ToString());

        using var conversation = await stranger.GetAsync(opened.Headers.Location, CancellationToken);
        using var messages = await stranger.GetAsync($"{opened.Headers.Location}/messages", CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, conversation.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, messages.StatusCode);
    }

    [Fact]
    public async Task UnknownConversationReturns404()
    {
        using var client = api.CreateClientFor(_externalSubject);

        using var conversation = await client.GetAsync($"/conversations/{Guid.NewGuid()}", CancellationToken);
        using var messages = await client.GetAsync($"/conversations/{Guid.NewGuid()}/messages", CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, conversation.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, messages.StatusCode);
    }

    [Fact]
    public async Task RequestWithoutTokenReturns401()
    {
        using var client = api.CreateClient();

        using var response = await client.GetAsync($"/conversations/{Guid.NewGuid()}", CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MessagesOfNewConversationContainFirstMessage()
    {
        using var client = api.CreateClientFor(_externalSubject);
        using var opened = await OpenAsync(client);

        using var response = await client.GetAsync($"{opened.Headers.Location}/messages", CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = Assert.IsType<MessagePageDto>(
            await response.Content.ReadFromJsonAsync<MessagePageDto>(CancellationToken));
        var message = Assert.Single(page.Items);
        Assert.Equal(1, message.Seq);
        Assert.Equal("Customer", message.AuthorType);
        Assert.Equal(FirstMessage, message.Body);
        Assert.False(page.HasMore);
    }

    [Theory]
    [InlineData("?afterSeq=1&beforeSeq=5")]
    [InlineData("?limit=0")]
    [InlineData("?limit=201")]
    [InlineData("?afterSeq=abc")]
    public async Task InvalidPageParametersReturn400(string queryString)
    {
        using var client = api.CreateClientFor(_externalSubject);
        using var opened = await OpenAsync(client);

        using var response = await client.GetAsync($"{opened.Headers.Location}/messages{queryString}", CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<HttpResponseMessage> OpenAsync(HttpClient client)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/conversations")
        {
            Content = JsonContent.Create(new { subject = Subject, firstMessage = FirstMessage }),
        };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var response = await client.SendAsync(request, CancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return response;
    }

    private sealed record ConversationDto(Guid Id, string Subject, string Status);

    private sealed record MessagePageDto(IReadOnlyList<MessageDto> Items, bool HasMore);

    private sealed record MessageDto(long Seq, string AuthorType, string Body);
}
