using PreOddsApi.ExternalApis.DependencyInjection;
using PreOddsApi.Worker;
using SportMonks.Core.Worker.WorkerServices;

var builder = Host.CreateDefaultBuilder(args);

// Wires Serilog (FromLogContext + console + file via configuration) and
// OpenTelemetry metrics + traces. Configuration overrides live in
// appsettings.json under the Serilog section.
WorkerObservability.Configure(builder, "PreOddsApi.Worker.Core");

builder.ConfigureServices((context, services) =>
{
    services.AddSportMonksApiClient(context.Configuration);
    services.AddHostedService<CoreWorkerService>();
});

await builder.Build().RunAsync();
