using Serilog;
using SportMonks.Core.Worker.WorkerServices;
using SportMonks.Core.Worker.Mapping;
using PreOddsApi.DataLayer;
using PreOddsApi.Core.Data.EntityFramework.Abstract;
using PreOddsApi.Core.Data.EntityFramework.Concrete;
using PreOddsApi.ExternalApis.DependencyInjection;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((configuration, services) =>
    {
        services.AddSportMonksApiClient(configuration.Configuration);
        //services.AddHostedService<Worker>();
        services.AddHostedService<CoreWorkerService>()
                .AddAutoMapper(typeof(CoreMapping))
                .AddSingleton(typeof(IUpsertService<>), typeof(UpsertService<>))
                .AddDbContext<PreOddsApiDbContext>(options => {
                    PreOddsDatabaseOptions.Configure(options, configuration.Configuration, useNoTracking: true);
                    }
                
                );
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
