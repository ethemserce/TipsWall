using Serilog;
using Microsoft.EntityFrameworkCore;
using SportMonks.Football.FixtureWorker.Services;
using SportMonks.Football.FixtureWorker.Mapping;
using PreOddsApi.DataLayer;
using SportMonks.Football.FootballWorker.Abstract;
using SportMonks.Football.FootballWorker.Concrete;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((configuration, services) =>
    {
        //services.AddHostedService<Worker>();
        services.AddHostedService<OddsWorkerService>()
                .AddAutoMapper(typeof(OddsMapping))
                //.AddSingleton<IUnitOfWork<PreOddsApiDbContext>, UnitOfWork<PreOddsApiDbContext>>()
                .AddSingleton<IInsertService, InsertService>()
              .AddDbContext<PreOddsApiDbContext>(options =>
                options.UseMySql(
                    configuration.Configuration.GetConnectionString("PreOddsApiMySqlDb"),
                     ServerVersion.AutoDetect(configuration.Configuration.GetConnectionString("PreOddsApiMySqlDb"))
                     ));
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