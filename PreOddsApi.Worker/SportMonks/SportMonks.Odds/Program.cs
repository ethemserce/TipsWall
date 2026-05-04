using Serilog;
using SportMonks.Football.FixtureWorker.Services;
using PreOddsApi.ExternalApis.DependencyInjection;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((configuration, services) =>
    {
        services.AddSportMonksApiClient(configuration.Configuration);
        services.AddHostedService<OddsWorkerService>();
    })
    //.ConfigureAppConfiguration((hostContext, configBuilder) =>
    //{
    //    configBuilder
    //           .SetBasePath(Directory.GetCurrentDirectory())
    //           .AddJsonFile("appsettings.json")
    //           .AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);

    //})
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
