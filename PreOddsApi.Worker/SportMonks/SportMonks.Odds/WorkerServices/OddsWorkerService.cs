using PreOddsApi.Entities.PreOddsEntities;
using PreOddsApi.Entities.SportMonks.Odds.V3;
using PreOddsApi.ExternalApis.SportMonks;
using SportMonks.Football.FootballWorker.Abstract;

namespace SportMonks.Football.FixtureWorker.Services
{
    public class OddsWorkerService : BackgroundService
    {
        private readonly ILogger<OddsWorkerService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IInsertService _insertService;

        public OddsWorkerService(ILogger<OddsWorkerService> logger, IConfiguration configuration,
            IServiceScopeFactory scopeFactory,
            IInsertService insertService)
        {
            _logger = logger;
            _configuration = configuration;
            _scopeFactory = scopeFactory;
            _insertService = insertService;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Fixture Service execution started!");
                try
                {
                    await _insertService.InsertAsync<Market, market>(await SportMonksApi.GetAll<Market>(_configuration, _logger, "markets"));

                    await _insertService.InsertAsync<Bookmaker, bookmaker>(await SportMonksApi.GetAll<Bookmaker>(_configuration, _logger, "bookmakers"));

                }
                catch (Exception exc)
                {
                    _logger.LogError(exc, exc.Message);
                }
                await Task.Delay(1000, stoppingToken);
            }
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Odds Service started!");
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Odds Service stppped!");
            return base.StopAsync(cancellationToken);
        }
    }
}
