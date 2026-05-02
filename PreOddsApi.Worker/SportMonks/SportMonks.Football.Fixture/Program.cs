using Serilog;
using SportMonks.Football.FixtureWorker.Services;
using SportMonks.Football.FixtureWorker.Mapping;
using PreOddsApi.DataLayer;
using PreOddsApi.Core.Data.EntityFramework.Abstract;
using PreOddsApi.Core.Data.EntityFramework.Concrete;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((configuration, services) =>
    {
        //services.AddHostedService<Worker>();
        services.AddHostedService<FootballWorkerService>()
                .AddAutoMapper(typeof(FootballMapping))
                .AddSingleton(typeof(IUpsertService<>), typeof(UpsertService<>))
              .AddDbContext<PreOddsApiDbContext>(options =>
                PreOddsDatabaseOptions.Configure(options, configuration.Configuration));
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

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("microsoft", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.File(configSetting["Logging:Logpath"])
    .CreateLogger();


host.Run();
