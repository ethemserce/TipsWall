using Serilog;
using SportMonks.Football.FixtureWorker.Services;
using SportMonks.Football.FixtureWorker.Mapping;
using PreOddsApi.DataLayer;
using SportMonks.Football.FootballWorker.Abstract;
using SportMonks.Football.FootballWorker.Concrete;
using PreOddsApi.ExternalApis.DependencyInjection;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((configuration, services) =>
    {
        services.AddSportMonksApiClient(configuration.Configuration);
        //services.AddHostedService<Worker>();
        services.AddHostedService<OddsWorkerService>()
                .AddAutoMapper(typeof(OddsMapping))
                //.AddSingleton<IUnitOfWork<PreOddsApiDbContext>, UnitOfWork<PreOddsApiDbContext>>()
                .AddSingleton<IInsertService, InsertService>()
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
