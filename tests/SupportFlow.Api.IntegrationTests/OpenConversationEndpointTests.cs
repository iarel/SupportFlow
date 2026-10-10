using System.Net;
using System.Net.Http.Json;
using Npgsql;

namespace SupportFlow.Api.IntegrationTests;

/// <summary>
/// <c>POST /conversations</c> through the API host: authentication, user account mapping (ADR-0016), routing to
/// the default team and HTTP mapping of the command (components.md §1.3).
/// </summary>
public sealed class OpenConversationEndpointTests(ApiFactory api)
{
    private const string Subject = "Payment failed";
    private const string FirstMessage = "My card was charged twice.";

    private readonly string _externalSubject = Guid.NewGuid().ToString();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task OpensConversationForNewCustomerInDefaultTeam()
    {
        using var client = api.CreateClientFor(_externalSubject);

        using var response = await PostAsync(client, Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var conversation = await ReadConversationAsync(response);
        Assert.Equal($"/conversations/{conversation.Id}", response.Headers.Location?.OriginalString);
        Assert.Equal("\"1\"", response.Headers.ETag?.Tag);
        Assert.Equal(Subject, conversation.Subject);
        Assert.Equal("New", conversation.Status);
        Assert.Equal("Normal", conversation.Priority);

        await using var connection = await api.OpenConnectionAsync(CancellationToken);
        var accountId = await ScalarAsync<Guid>(
            connection,
            "SELECT id FROM identity.user_accounts WHERE external_issuer = $1 AND external_subject = $2 AND account_type = 'Customer'",
            ApiFactory.Issuer,
            _externalSubject);
        Assert.Equal(
            accountId,
            await ScalarAsync<Guid>(connection, "SELECT customer_id FROM conversations.conversations WHERE id = $1", conversation.Id));
        Assert.Equal(
            await ScalarAsync<Guid>(connection, "SELECT id FROM organization.teams WHERE is_default"),
            await ScalarAsync<Guid>(connection, "SELECT team_id FROM conversations.conversations WHERE id = $1", conversation.Id));
    }

    [Fact]
    public async Task RepeatedRequestReturnsSameConversationWith200()
    {
        using var client = api.CreateClientFor(_externalSubject);
        var idempotencyKey = Guid.NewGuid();

        using var first = await PostAsync(client, idempotencyKey);
        using var repeated = await PostAsync(client, idempotencyKey);

        Assert.Equal(HttpStatusCode.OK, repeated.StatusCode);
        Assert.Equal((await ReadConversationAsync(first)).Id, (await ReadConversationAsync(repeated)).Id);
    }

    [Fact]
    public async Task ReusedKeyWithDifferentBodyReturns422()
    {
        using var client = api.CreateClientFor(_externalSubject);
        var idempotencyKey = Guid.NewGuid();
        using var first = await PostAsync(client, idempotencyKey);

        using var response = await PostAsync(client, idempotencyKey, firstMessage: "Another message");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-uuid")]
    public async Task MissingOrInvalidIdempotencyKeyReturns400(string? idempotencyKey)
    {
        using var client = api.CreateClientFor(_externalSubject);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/conversations")
        {
            Content = JsonContent.Create(new { Subject, FirstMessage }),
        };

        if (idempotencyKey is not null)
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }

        using var response = await client.SendAsync(request, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task EmptySubjectReturns400()
    {
        using var client = api.CreateClientFor(_externalSubject);

        using var response = await PostAsync(client, Guid.NewGuid(), subject: "   ");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task BodyOver16KilobytesReturns413()
    {
        using var client = api.CreateClientFor(_externalSubject);

        using var response = await PostAsync(client, Guid.NewGuid(), firstMessage: new string('a', 17 * 1024));

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }

    [Fact]
    public async Task RequestWithoutTokenReturns401()
    {
        using var client = api.CreateClient();

        using var response = await PostAsync(client, Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task InactiveAccountReturns403()
    {
        using var client = api.CreateClientFor(_externalSubject);
        using var first = await PostAsync(client, Guid.NewGuid());

        await using var connection = await api.OpenConnectionAsync(CancellationToken);
        await ExecuteAsync(
            connection,
            "UPDATE identity.user_accounts SET is_active = false WHERE external_subject = $1",
            _externalSubject);

        using var response = await PostAsync(client, Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ParallelFirstRequestsCreateOneAccount()
    {
        using var client = api.CreateClientFor(_externalSubject);

        var responses = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => PostAsync(client, Guid.NewGuid())));

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.Created, r.StatusCode));
        await using var connection = await api.OpenConnectionAsync(CancellationToken);
        Assert.Equal(
            1,
            await ScalarAsync<long>(
                connection,
                "SELECT count(*) FROM identity.user_accounts WHERE external_subject = $1",
                _externalSubject));

        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    [Fact]
    public async Task ExceedingDailyLimitReturns429WithRetryAfter()
    {
        using var client = api.CreateClientFor(_externalSubject);

        for (var i = 0; i < 10; i++)
        {
            using var opened = await PostAsync(client, Guid.NewGuid());
            Assert.Equal(HttpStatusCode.Created, opened.StatusCode);
        }

        using var response = await PostAsync(client, Guid.NewGuid());

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        var retryAfter = Assert.NotNull(response.Headers.RetryAfter?.Delta);
        Assert.InRange(retryAfter, TimeSpan.FromHours(23), TimeSpan.FromHours(24));
    }

    [Fact]
    public async Task RequestBlockedByConcurrentTransactionReturns503AfterLockTimeout()
    {
        using var client = api.CreateClientFor(_externalSubject);
        using var first = await PostAsync(client, Guid.NewGuid());
        var idempotencyKey = Guid.NewGuid();

        // A concurrent request that registered the same key and does not finish.
        await using var connection = await api.OpenConnectionAsync(CancellationToken);
        await using var concurrent = await connection.BeginTransactionAsync(CancellationToken);
        await ExecuteAsync(
            connection,
            """
            INSERT INTO conversations.idempotency_keys (caller_id, key, operation, request_hash, resource_id, created_at)
            SELECT id, $2, 'OpenConversation', '\x00'::bytea, gen_random_uuid(), now()
            FROM identity.user_accounts WHERE external_subject = $1
            """,
            _externalSubject,
            idempotencyKey);

        using var response = await PostAsync(client, idempotencyKey);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(TimeSpan.FromSeconds(1), response.Headers.RetryAfter?.Delta);
    }

    private static Task<HttpResponseMessage> PostAsync(
        HttpClient client,
        Guid idempotencyKey,
        string subject = Subject,
        string firstMessage = FirstMessage)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/conversations")
        {
            Content = JsonContent.Create(new { subject, firstMessage }),
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey.ToString());

        return client.SendAsync(request, CancellationToken);
    }

    private static async Task<ConversationDto> ReadConversationAsync(HttpResponseMessage response)
    {
        return Assert.IsType<ConversationDto>(
            await response.Content.ReadFromJsonAsync<ConversationDto>(CancellationToken));
    }

    private static async Task<T> ScalarAsync<T>(NpgsqlConnection connection, string sql, params object[] parameters)
    {
        await using var command = CreateCommand(connection, sql, parameters);
        return (T)(await command.ExecuteScalarAsync(CancellationToken))!;
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql, params object[] parameters)
    {
        await using var command = CreateCommand(connection, sql, parameters);
        await command.ExecuteNonQueryAsync(CancellationToken);
    }

    private static NpgsqlCommand CreateCommand(NpgsqlConnection connection, string sql, object[] parameters)
    {
        var command = new NpgsqlCommand(sql, connection);

        foreach (var parameter in parameters)
        {
            command.Parameters.Add(new NpgsqlParameter { Value = parameter });
        }

        return command;
    }

    private sealed record ConversationDto(Guid Id, string Subject, string Status, string Priority);
}
