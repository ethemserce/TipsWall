using PreOddsApi.Entities.SportMonks.Football.V3;
using PreOddsApi.ExternalApis.SportMonks;
using PreOddsApi.ExternalApis.SportMonks.Sync;
using PreOddsApi.ExternalApis.SportMonks.Sync.Writers;

namespace SportMonks.Football.FixtureWorker.Services
{
    public class FootballWorkerService : BackgroundService
    {
        private readonly ILogger<FootballWorkerService> _logger;
        private readonly ISportMonksSyncRunner _syncRunner;
        private readonly ISportMonksCompetitionReferenceWriter _competitionReferenceWriter;

        public FootballWorkerService(
            ILogger<FootballWorkerService> logger,
            ISportMonksSyncRunner syncRunner,
            ISportMonksCompetitionReferenceWriter competitionReferenceWriter)
        {
            _logger = logger;
            _syncRunner = syncRunner;
            _competitionReferenceWriter = competitionReferenceWriter;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Fixture Service execution started!");

                try
                {
                    await ExecuteLeague(stoppingToken);
                }
                catch (Exception exc)
                {
                    _logger.LogError(exc, exc.Message);
                }

                await Task.Delay(100000, stoppingToken);
            }
        }

        private async Task ExecuteLeague(CancellationToken cancellationToken)
        {
            var leagues = (await _syncRunner.GetAllAsync<League>(
                SportMonksSyncJobDefinition.Create(
                    "sportmonks.football.leagues",
                    "competition.league",
                    "Sync SportMonks football leagues with sport, seasons, stages, rounds, and groups."),
                SportMonksApiRequest.Create("leagues")
                    .WithInclude(
                        "sport",
                        "seasons",
                        "stages",
                        "stages.rounds",
                        "stages.groups",
                        "seasons.groups"),
                cancellationToken: cancellationToken)).ToList();

            await _competitionReferenceWriter.UpsertLeaguesWithHierarchyAsync(leagues, cancellationToken);
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fixture Service started!");
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fixture Service stopped!");
            return base.StopAsync(cancellationToken);
        }
    }
}
