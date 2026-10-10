using System.Net;

namespace SupportFlow.Api.IntegrationTests;

/// <summary>
/// Orchestrator probes (containers.md, Observability): anonymous, liveness without dependencies, readiness checks
/// PostgreSQL.
/// </summary>
public sealed class HealthEndpointTests(ApiFactory api)
{
    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task ProbeIsHealthyWithoutToken(string path)
    {
        using var client = api.CreateClient();

        using var response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }
}
