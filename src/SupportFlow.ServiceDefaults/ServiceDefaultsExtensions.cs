using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace SupportFlow.ServiceDefaults;

/// <summary>
/// Observability and health checks shared by the API and the Worker (containers.md, Observability).
/// </summary>
public static class ServiceDefaultsExtensions
{
    /// <summary>
    /// Activity sources and meters of SupportFlow code, such as <c>SupportFlow.Outbox</c>.
    /// </summary>
    public const string TelemetryPrefix = "SupportFlow.*";

    private const string LiveTag = "live";
    private const string ReadyTag = "ready";

    /// <summary>
    /// OpenTelemetry traces, metrics and logs, configured by the standard <c>OTEL_*</c> variables. Telemetry is
    /// exported over OTLP only when <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> is set.
    /// </summary>
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        var openTelemetry = builder.Services.AddOpenTelemetry()
            .WithTracing(tracing => tracing
                .AddSource(builder.Environment.ApplicationName)
                .AddSource(TelemetryPrefix)
                .AddAspNetCoreInstrumentation(options => options.Filter = context => !IsHealthCheck(context))
                .AddHttpClientInstrumentation()
                .AddNpgsql())
            .WithMetrics(metrics => metrics
                .AddMeter(TelemetryPrefix)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddNpgsqlInstrumentation());

        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            openTelemetry.UseOtlpExporter();
        }

        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), [LiveTag])
            .AddCheck<PostgresHealthCheck>("postgres", tags: [ReadyTag]);

        return builder;
    }

    /// <summary>
    /// <c>/health/live</c>: the process runs, no dependencies. <c>/health/ready</c>: PostgreSQL is reachable.
    /// Used by orchestrator probes; anonymous.
    /// </summary>
    public static WebApplication MapHealthEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = check => check.Tags.Contains(LiveTag) });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains(ReadyTag) });

        return app;
    }

    private static bool IsHealthCheck(HttpContext context)
    {
        return context.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase);
    }
}
