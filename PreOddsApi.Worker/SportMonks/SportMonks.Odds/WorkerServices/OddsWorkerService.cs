using PreOddsApi.Entities.PreOddsEntities;
using PreOddsApi.Entities.SportMonks.Odds.V3;
using PreOddsApi.ExternalApis.SportMonks;
using SportMonks.Football.FootballWorker.Abstract;

namespace SportMonks.Football.FixtureWorker.Services
{
    public class OddsWorkerService : BackgroundService
    {
        private readonly ILogger<OddsWorkerService> _logger;
        private readonly ISportMonksApiClient _sportMonksApiClient;
        private readonly IInsertService _insertService;

        public OddsWorkerService(ILogger<OddsWorkerService> logger,
            ISportMonksApiClient sportMonksApiClient,
            IInsertService insertService)
        {
            _logger = logger;
            _sportMonksApiClient = sportMonksApiClient;
            _insertService = insertService;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Fixture Service execution started!");
                try
                {
                    var markets = (await _sportMonksApiClient.GetAllAsync<Market>(
                        SportMonksApiRequest.Create("markets"),
                        stoppingToken)).ToList();
                    await _insertService.InsertAsync<Market, market>(markets);

                    var bookmakers = (await _sportMonksApiClient.GetAllAsync<Bookmaker>(
                        SportMonksApiRequest.Create("bookmakers"),
                        stoppingToken)).ToList();
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
