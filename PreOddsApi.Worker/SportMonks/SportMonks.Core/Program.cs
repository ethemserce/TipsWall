using Serilog;
using Microsoft.EntityFrameworkCore;
using SportMonks.Core.Worker.WorkerServices;
using SportMonks.Core.Worker.Mapping;
using PreOddsApi.DataLayer;
using Microsoft.Extensions.Options;
using PreOddsApi.Core.Data.EntityFramework.Abstract;
using PreOddsApi.Core.Data.EntityFramework.Concrete;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((configuration, services) =>
    {
        //services.AddHostedService<Worker>();
        services.AddHostedService<CoreWorkerService>()
                .AddAutoMapper(typeof(CoreMapping))
                .AddSingleton(typeof(IUpsertService<>), typeof(UpsertService<>))
                .AddDbContext<PreOddsApiDbContext>(options => {
                    options.UseMySql(
                        configuration.Configuration.GetConnectionString("PreOddsApiMySqlDb"),
                         ServerVersion.AutoDetect(configuration.Configuration.GetConnectionString("PreOddsApiMySqlDb"))
                         );
                    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
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