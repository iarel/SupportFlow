using System.Diagnostics;
using Npgsql;

namespace SupportFlow.Api.IntegrationTests;

/// <summary>
/// ADR-0017: outside Development the schema is created by the <c>migrate</c> deployment step, which runs the API
/// binary, applies the migrations of all modules and exits.
/// </summary>
public sealed class MigrateCommandTests(ApiFactory api)
{
    [Fact]
    public async Task MigrateAppliesMigrationsOfAllModulesAndExits()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var connectionString = await api.CreateDatabaseAsync("migrate_step", cancellationToken);

        var exitCode = await RunMigrateAsync(connectionString, cancellationToken);

        Assert.Equal(0, exitCode);
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        foreach (var table in new[] { "conversations.conversations", "identity.user_accounts", "organization.teams", "audit.audit_events" })
        {
            await using var command = new NpgsqlCommand("SELECT to_regclass(@table) IS NOT NULL", connection);
            command.Parameters.AddWithValue("table", table);
            Assert.True((bool)(await command.ExecuteScalarAsync(cancellationToken))!, $"{table} is missing");
        }
    }

    private static async Task<int> RunMigrateAsync(string connectionString, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            ArgumentList = { typeof(Program).Assembly.Location, "migrate" },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
        startInfo.Environment["DOTNET_ENVIRONMENT"] = "Production";
        startInfo.Environment["ConnectionStrings__SupportFlow"] = connectionString;
        startInfo.Environment.Remove("OTEL_EXPORTER_OTLP_ENDPOINT");

        using var process = Process.Start(startInfo)!;
        var output = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var error = process.StandardError.ReadToEndAsync(cancellationToken);

        // The step must exit by itself; a host that keeps serving would hang the deployment.
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(1));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }

        TestContext.Current.TestOutputHelper?.WriteLine(await output + await error);
        return process.ExitCode;
    }
}
