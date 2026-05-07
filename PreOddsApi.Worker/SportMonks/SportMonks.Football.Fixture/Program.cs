using PreOddsApi.ExternalApis.DependencyInjection;
using Serilog;
using SportMonks.Football.FixtureWorker.Services;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((configuration, services) =>
    {
        services.AddSportMonksApiClient(configuration.Configuration);

        // Live bridge — worker → WebApi → SignalR clients.
        services.AddHttpClient<IFixtureLiveBridge, HttpFixtureLiveBridge>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(5);
        });

        services.AddHostedService<FootballWorkerService>();
    })
    .UseSerilog()
    .Build();

var configSetting = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();
var logPath = configSetting["Logging:Logpath"] ?? "Logs/Api_logs.txt";

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("microsoft", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.File(logPath)
    .CreateLogger();

host.Run();
