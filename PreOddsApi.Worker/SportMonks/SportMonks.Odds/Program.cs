using PreOddsApi.ExternalApis.DependencyInjection;
using PreOddsApi.Worker;
using SportMonks.Football.FixtureWorker.Services;

var builder = Host.CreateDefaultBuilder(args);

WorkerObservability.Configure(builder, "PreOddsApi.Worker.Odds");

builder.ConfigureServices((context, services) =>
{
    services.AddSportMonksApiClient(context.Configuration);
    services.AddHostedService<OddsWorkerService>();
});

await builder.Build().RunAsync();
