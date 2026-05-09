using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace PreOddsApi.Worker
{
    /// <summary>
    /// Shared observability setup for the SportMonks workers (Core, Football,
    /// Odds). Mirrors the WebApi setup so a single dashboard / log query can
    /// span the entire fleet.
    ///
    /// Wires:
    ///   - Serilog with FromLogContext, console + file sinks. Configuration
    ///     comes from appsettings (Serilog section).
    ///   - OpenTelemetry metrics + tracing: HttpClient (SportMonks calls),
    ///     runtime, Npgsql source. Prometheus scrape endpoint isn't
    ///     mounted because workers don't host HTTP; export via OTLP later
    ///     if needed.
    /// </summary>
    public static class WorkerObservability
    {
        public static void Configure(IHostBuilder hostBuilder, string serviceName)
        {
            // Serilog bootstrap so even early DI failures get a log line.
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .CreateBootstrapLogger();

            hostBuilder.UseSerilog((ctx, services, cfg) => cfg
                .ReadFrom.Configuration(ctx.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.Console(
                    outputTemplate:
                        "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}"));

            hostBuilder.ConfigureServices((ctx, services) =>
            {
                var resource = ResourceBuilder.CreateDefault()
                    .AddService(serviceName, serviceVersion: "1.0")
                    .AddAttributes(new[]
                    {
                        new System.Collections.Generic.KeyValuePair<string, object>(
                            "deployment.environment", ctx.HostingEnvironment.EnvironmentName)
                    });

                services.AddOpenTelemetry()
                    .WithMetrics(m => m
                        .SetResourceBuilder(resource)
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation()
                        .AddMeter("Npgsql"))
                    .WithTracing(t => t
                        .SetResourceBuilder(resource)
                        .AddHttpClientInstrumentation()
                        // Same workaround as the WebApi: AddNpgsql() collides
                        // with Npgsql.EntityFrameworkCore.PostgreSQL's
                        // identically-named extension. Subscribing to the
                        // source by name yields the same telemetry.
                        .AddSource("Npgsql"));
            });
        }
    }
}
