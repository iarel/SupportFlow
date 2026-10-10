using System.Net.Http.Headers;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using SupportFlow.Api.IntegrationTests;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(ApiFactory))]

namespace SupportFlow.Api.IntegrationTests;

/// <summary>
/// The API host with all modules on a PostgreSQL container (requires Docker). The Development environment applies
/// the migrations on startup. Tokens are signed by a test key instead of an Identity Provider.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string Issuer = "https://idp.test";

    private const string Audience = "supportflow-api";

    private readonly SymmetricSecurityKey _signingKey = new(RandomNumberGenerator.GetBytes(32));
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        // Starts the host, which applies the migrations.
        _ = Server;
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _container.DisposeAsync();
    }

    public HttpClient CreateClientFor(string subject)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(subject));
        return client;
    }

    public async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    /// <summary>
    /// Creates an empty database next to the API's own and returns its connection string.
    /// </summary>
    public async Task<string> CreateDatabaseAsync(string name, CancellationToken cancellationToken)
    {
        await using (var connection = await OpenConnectionAsync(cancellationToken))
        {
            await using var command = new NpgsqlCommand($"CREATE DATABASE {name}", connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        return new NpgsqlConnectionStringBuilder(_container.GetConnectionString()) { Database = name }.ConnectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:SupportFlow"] = _container.GetConnectionString(),
            }));

        builder.ConfigureTestServices(services => services.PostConfigure<JwtBearerOptions>(
            JwtBearerDefaults.AuthenticationScheme,
            options => options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = Issuer,
                ValidAudience = Audience,
                IssuerSigningKey = _signingKey,
            }));
    }

    private string CreateToken(string subject)
    {
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Expires = DateTime.UtcNow.AddMinutes(10),
            Claims = new Dictionary<string, object> { ["sub"] = subject },
            SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256),
        });
    }
}
