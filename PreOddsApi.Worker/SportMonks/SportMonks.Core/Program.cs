using PreOddsApi.ExternalApis.DependencyInjection;
using Serilog;
using SportMonks.Core.Worker.WorkerServices;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((configuration, services) =>
    {
        services.AddSportMonksApiClient(configuration.Configuration);
        services.AddHostedService<CoreWorkerService>();
    })
    .UseSerilog()
    .Build();

var configSetting = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("microsoft", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.File(configSetting["Logging:Logpath"])
    .CreateLogger();

host.Run();
