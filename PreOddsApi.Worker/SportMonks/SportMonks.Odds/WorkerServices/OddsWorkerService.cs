using PreOddsApi.Entities.PreOddsEntities;
using PreOddsApi.Entities.SportMonks.Odds.V3;
using PreOddsApi.ExternalApis.SportMonks;
using PreOddsApi.ExternalApis.SportMonks.Sync;
using SportMonks.Football.FootballWorker.Abstract;

namespace SportMonks.Football.FixtureWorker.Services
{
    public class OddsWorkerService : BackgroundService
    {
        private readonly ILogger<OddsWorkerService> _logger;
        private readonly ISportMonksSyncRunner _syncRunner;
        private readonly IInsertService _insertService;

        public OddsWorkerService(ILogger<OddsWorkerService> logger,
            ISportMonksSyncRunner syncRunner,
            IInsertService insertService)
        {
            _logger = logger;
            _syncRunner = syncRunner;
            _insertService = insertService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Fixture Service execution started!");
                try
                {
                    var markets = (await _syncRunner.GetAllAsync<Market>(
                        SportMonksSyncJobDefinition.Create(
                            "sportmonks.odds.markets",
                            "odds.market",
                            "Sync SportMonks odds markets."),
                        SportMonksApiRequest.Create("markets"),
                        cancellationToken: stoppingToken)).ToList();
                    await _insertService.InsertAsync<Market, market>(markets);

                    var bookmakers = (await _syncRunner.GetAllAsync<Bookmaker>(
                        SportMonksSyncJobDefinition.Create(
                            "sportmonks.odds.bookmakers",
                            "odds.bookmaker",
                            "Sync SportMonks odds bookmakers."),
                        SportMonksApiRequest.Create("bookmakers"),
                        cancellationToken: stoppingToken)).ToList();
                    await _insertService.InsertAsync<Bookmaker, bookmaker>(bookmakers);
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
