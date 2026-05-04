using System.Globalization;
using Microsoft.Extensions.Configuration;
using PreOddsApi.Entities.SportMonks.Odds.V3;
using PreOddsApi.ExternalApis.SportMonks;
using PreOddsApi.ExternalApis.SportMonks.Sync;
using PreOddsApi.ExternalApis.SportMonks.Sync.Writers;

namespace SportMonks.Football.FixtureWorker.Services
{
    public class OddsWorkerService : BackgroundService
    {
        private const string OddsReferenceKey = "worker.odds.reference";

        private readonly ILogger<OddsWorkerService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ISyncJobScheduler _scheduler;
        private readonly ISportMonksSyncRunner _syncRunner;
        private readonly ISportMonksOddsReferenceWriter _oddsReferenceWriter;

        public OddsWorkerService(
            ILogger<OddsWorkerService> logger,
            IConfiguration configuration,
            ISyncJobScheduler scheduler,
            ISportMonksSyncRunner syncRunner,
            ISportMonksOddsReferenceWriter oddsReferenceWriter)
        {
            _logger = logger;
            _configuration = configuration;
            _scheduler = scheduler;
            _syncRunner = syncRunner;
            _oddsReferenceWriter = oddsReferenceWriter;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var pollingInterval = TimeSpan.FromSeconds(
                GetInteger("SportMonksOddsWorkerSettings:PollingIntervalSeconds", 60));

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Odds worker polling at {Time}.", DateTimeOffset.UtcNow);

                try
                {
                    await MaybeRunOddsReferenceAsync(stoppingToken);
                }
                catch (Exception exc)
                {
                    _logger.LogError(exc, exc.Message);
                }

                await Task.Delay(pollingInterval, stoppingToken);
            }
        }

        private async Task MaybeRunOddsReferenceAsync(CancellationToken cancellationToken)
        {
            var interval = GetInteger("SportMonksOddsWorkerSettings:OddsReferenceIntervalSeconds", 86400);
            if (!_scheduler.ShouldRun(OddsReferenceKey, interval))
                return;

            var markets = (await _syncRunner.GetAllAsync<Market>(
                SportMonksSyncJobDefinition.Create(
                    "sportmonks.odds.markets",
                    "odds.market",
                    "Sync SportMonks odds markets."),
                SportMonksApiRequest.Create("markets"),
                cancellationToken: cancellationToken)).ToList();

            await _oddsReferenceWriter.UpsertMarketsAsync(markets, cancellationToken);

            var bookmakers = (await _syncRunner.GetAllAsync<Bookmaker>(
                SportMonksSyncJobDefinition.Create(
                    "sportmonks.odds.bookmakers",
                    "odds.bookmaker",
                    "Sync SportMonks odds bookmakers."),
                SportMonksApiRequest.Create("bookmakers"),
                cancellationToken: cancellationToken)).ToList();

            await _oddsReferenceWriter.UpsertBookmakersAsync(bookmakers, cancellationToken);

            _scheduler.RecordRun(OddsReferenceKey);
        }

        private int GetInteger(string key, int defaultValue)
        {
            return int.TryParse(
                _configuration[key],
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var result)
                ? result
                : defaultValue;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Odds worker started.");
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Odds worker stopped.");
            return base.StopAsync(cancellationToken);
        }
    }
}
