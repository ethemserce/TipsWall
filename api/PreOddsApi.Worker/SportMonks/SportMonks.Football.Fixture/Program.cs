using PreOddsApi.ExternalApis.DependencyInjection;
using PreOddsApi.Worker;
using SportMonks.Football.FixtureWorker.Services;

var builder = Host.CreateDefaultBuilder(args);

WorkerObservability.Configure(builder, "PreOddsApi.Worker.Football");

builder.ConfigureServices((context, services) =>
{
    services.AddSportMonksApiClient(context.Configuration);

    // Live bridge — worker → WebApi → SignalR clients.
    services.AddHttpClient<IFixtureLiveBridge, HttpFixtureLiveBridge>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(5);
    });

    services.AddHostedService<FootballWorkerService>();
});

await builder.Build().RunAsync();
