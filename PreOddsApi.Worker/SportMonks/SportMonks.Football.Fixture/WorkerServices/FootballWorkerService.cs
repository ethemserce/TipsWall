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
        private readonly ISportMonksFootballCoreReferenceWriter _footballCoreReferenceWriter;

        public FootballWorkerService(
            ILogger<FootballWorkerService> logger,
            ISportMonksSyncRunner syncRunner,
            ISportMonksCompetitionReferenceWriter competitionReferenceWriter,
            ISportMonksFootballCoreReferenceWriter footballCoreReferenceWriter)
        {
            _logger = logger;
            _syncRunner = syncRunner;
            _competitionReferenceWriter = competitionReferenceWriter;
            _footballCoreReferenceWriter = footballCoreReferenceWriter;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Fixture Service execution started!");

                try
                {
                    await ExecuteStates(stoppingToken);
                    await ExecuteVenues(stoppingToken);
                    await ExecuteLeague(stoppingToken);
                    await ExecuteTeams(stoppingToken);
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

        private async Task ExecuteStates(CancellationToken cancellationToken)
        {
            var states = (await _syncRunner.GetAllAsync<State>(
                SportMonksSyncJobDefinition.Create(
                    "sportmonks.football.states",
                    "catalog.state",
                    "Sync SportMonks football states with type include."),
                SportMonksApiRequest.Create("states")
                    .WithInclude("type"),
                cancellationToken: cancellationToken)).ToList();

            await _footballCoreReferenceWriter.UpsertStatesAsync(states, cancellationToken);
        }

        private async Task ExecuteVenues(CancellationToken cancellationToken)
        {
            var venues = (await _syncRunner.GetAllAsync<Venue>(
                SportMonksSyncJobDefinition.Create(
                    "sportmonks.football.venues",
                    "football.venue",
                    "Sync SportMonks football venues."),
                SportMonksApiRequest.Create("venues")
                    .WithInclude("country", "city"),
                cancellationToken: cancellationToken)).ToList();

            await _footballCoreReferenceWriter.UpsertVenuesAsync(venues, cancellationToken);
        }

        private async Task ExecuteTeams(CancellationToken cancellationToken)
        {
            var teams = (await _syncRunner.GetAllAsync<Team>(
                SportMonksSyncJobDefinition.Create(
                    "sportmonks.football.teams",
                    "football.team",
                    "Sync SportMonks football teams with sport and venue include."),
                SportMonksApiRequest.Create("teams")
                    .WithInclude("sport", "venue"),
                cancellationToken: cancellationToken)).ToList();

            await _footballCoreReferenceWriter.UpsertTeamsWithVenuesAsync(teams, cancellationToken);
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
