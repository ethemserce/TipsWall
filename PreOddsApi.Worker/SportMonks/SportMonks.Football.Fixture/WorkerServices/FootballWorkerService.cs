using System.Globalization;
using Microsoft.Extensions.Configuration;
using PreOddsApi.Entities.SportMonks.Football;
using PreOddsApi.Entities.SportMonks.Football.V3;
using PreOddsApi.ExternalApis.SportMonks;
using PreOddsApi.ExternalApis.SportMonks.Sync;
using PreOddsApi.ExternalApis.SportMonks.Sync.Writers;

namespace SportMonks.Football.FixtureWorker.Services
{
    public class FootballWorkerService : BackgroundService
    {
        private static readonly string[] FixtureSyncIncludes =
        [
            "sport",
            "league",
            "season",
            "stage",
            "group",
            "round",
            "state",
            "venue",
            "participants",
            "scores",
            "periods",
            "events",
            "statistics",
            "lineups",
            "lineups.details.type",
            "formations",
            "referees"
        ];

        private static readonly string[] FixtureSidelinedIncludes =
        [
            "sidelined",
            "sidelined.player",
            "sidelined.type",
            "sidelined.participant",
            "sidelined.sideline"
        ];

        private static readonly string[] TvStationIncludes =
        [
            "countries"
        ];

        private static readonly string[] PlayerReferenceIncludes =
        [
            "sport",
            "country",
            "city",
            "nationality"
        ];

        private static readonly string[] CoachReferenceIncludes =
        [
            "country",
            "player"
        ];

        private static readonly string[] RivalReferenceIncludes =
        [
            "team",
            "rival"
        ];

        private static readonly string[] TeamSquadIncludes =
        [
            "team",
            "player"
        ];

        private static readonly string[] StandingIncludes =
        [
            "participant",
            "sport",
            "league",
            "season",
            "stage",
            "group",
            "round",
            "rule.type",
            "details.type",
            "form"
        ];

        private static readonly string[] TopScorerIncludes =
        [
            "season",
            "stage",
            "player",
            "participant",
            "type"
        ];

        private static readonly string[] TransferIncludes =
        [
            "sport",
            "player",
            "type",
            "fromTeam",
            "toTeam",
            "position",
            "detailedPosition"
        ];

        private static readonly string[] NewsIncludes =
        [
            "lines"
        ];

        private readonly ILogger<FootballWorkerService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ISportMonksSyncRunner _syncRunner;
        private readonly ISportMonksCompetitionReferenceWriter _competitionReferenceWriter;
        private readonly ISportMonksFootballCoreReferenceWriter _footballCoreReferenceWriter;
        private readonly ISportMonksFixtureCoreWriter _fixtureCoreWriter;
        private readonly ISportMonksFixtureEventStatisticWriter _fixtureEventStatisticWriter;
        private readonly ISportMonksFixtureLineupFormationWriter _fixtureLineupFormationWriter;
        private readonly ISportMonksFixtureRefereeWriter _fixtureRefereeWriter;
        private readonly ISportMonksPlayerCoachSquadRivalWriter _playerCoachSquadRivalWriter;
        private readonly ISportMonksStandingTopScorerWriter _standingTopScorerWriter;
        private readonly ISportMonksTransferSidelinedWriter _transferSidelinedWriter;
        private readonly ISportMonksFixtureMediaWeatherWriter _fixtureMediaWeatherWriter;
        private readonly ISportMonksFixtureTrendCommentaryWriter _fixtureTrendCommentaryWriter;
        private readonly ISportMonksNewsWriter _newsWriter;

        public FootballWorkerService(
            ILogger<FootballWorkerService> logger,
            IConfiguration configuration,
            ISportMonksSyncRunner syncRunner,
            ISportMonksCompetitionReferenceWriter competitionReferenceWriter,
            ISportMonksFootballCoreReferenceWriter footballCoreReferenceWriter,
            ISportMonksFixtureCoreWriter fixtureCoreWriter,
            ISportMonksFixtureEventStatisticWriter fixtureEventStatisticWriter,
            ISportMonksFixtureLineupFormationWriter fixtureLineupFormationWriter,
            ISportMonksFixtureRefereeWriter fixtureRefereeWriter,
            ISportMonksPlayerCoachSquadRivalWriter playerCoachSquadRivalWriter,
            ISportMonksStandingTopScorerWriter standingTopScorerWriter,
            ISportMonksTransferSidelinedWriter transferSidelinedWriter,
            ISportMonksFixtureMediaWeatherWriter fixtureMediaWeatherWriter,
            ISportMonksFixtureTrendCommentaryWriter fixtureTrendCommentaryWriter,
            ISportMonksNewsWriter newsWriter)
        {
            _logger = logger;
            _configuration = configuration;
            _syncRunner = syncRunner;
            _competitionReferenceWriter = competitionReferenceWriter;
            _footballCoreReferenceWriter = footballCoreReferenceWriter;
            _fixtureCoreWriter = fixtureCoreWriter;
            _fixtureEventStatisticWriter = fixtureEventStatisticWriter;
            _fixtureLineupFormationWriter = fixtureLineupFormationWriter;
            _fixtureRefereeWriter = fixtureRefereeWriter;
            _playerCoachSquadRivalWriter = playerCoachSquadRivalWriter;
            _standingTopScorerWriter = standingTopScorerWriter;
            _transferSidelinedWriter = transferSidelinedWriter;
            _fixtureMediaWeatherWriter = fixtureMediaWeatherWriter;
            _fixtureTrendCommentaryWriter = fixtureTrendCommentaryWriter;
            _newsWriter = newsWriter;
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
                    var leagues = await ExecuteLeague(stoppingToken);
                    var teams = await ExecuteTeams(stoppingToken);
                    await ExecutePlayerCoachSquadRivalReferences(teams, stoppingToken);
                    await ExecuteStandingTopScorerReferences(leagues, stoppingToken);
                    await ExecuteTransferReferences(stoppingToken);
                    await ExecuteTvStationReferences(stoppingToken);
                    await ExecuteNewsReferences(stoppingToken);
                    await ExecuteFixtureWindow(stoppingToken);
                }
                catch (Exception exc)
                {
                    _logger.LogError(exc, exc.Message);
                }

                await Task.Delay(100000, stoppingToken);
            }
        }

        private async Task<List<League>> ExecuteLeague(CancellationToken cancellationToken)
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
                        "currentSeason",
                        "stages",
                        "stages.rounds",
                        "stages.groups",
                        "seasons.groups"),
                cancellationToken: cancellationToken)).ToList();

            await _competitionReferenceWriter.UpsertLeaguesWithHierarchyAsync(leagues, cancellationToken);
            return leagues;
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

        private async Task<List<Team>> ExecuteTeams(CancellationToken cancellationToken)
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
            return teams;
        }

        private async Task ExecutePlayerCoachSquadRivalReferences(
            IReadOnlyCollection<Team> teams,
            CancellationToken cancellationToken)
        {
            if (!GetBoolean("SportMonksPlayerReferenceSync:Enabled", false))
            {
                return;
            }

            if (GetBoolean("SportMonksPlayerReferenceSync:SyncPlayers", true))
            {
                var players = (await _syncRunner.GetAllAsync<Player>(
                    SportMonksSyncJobDefinition.Create(
                        "sportmonks.football.players",
                        "football.player",
                        "Sync SportMonks football player reference data."),
                    SportMonksApiRequest.Create("players")
                        .WithInclude(PlayerReferenceIncludes),
                    cancellationToken: cancellationToken)).ToList();

                await _playerCoachSquadRivalWriter.UpsertPlayersAsync(players, cancellationToken);
            }

            if (GetBoolean("SportMonksPlayerReferenceSync:SyncCoaches", true))
            {
                var coaches = (await _syncRunner.GetAllAsync<Coach>(
                    SportMonksSyncJobDefinition.Create(
                        "sportmonks.football.coaches",
                        "football.coach",
                        "Sync SportMonks football coach reference data."),
                    SportMonksApiRequest.Create("coaches")
                        .WithInclude(CoachReferenceIncludes),
                    cancellationToken: cancellationToken)).ToList();

                await _playerCoachSquadRivalWriter.UpsertCoachesAsync(coaches, cancellationToken);
            }

            if (GetBoolean("SportMonksPlayerReferenceSync:SyncRivals", true))
            {
                var rivals = (await _syncRunner.GetAllAsync<Rival>(
                    SportMonksSyncJobDefinition.Create(
                        "sportmonks.football.rivals",
                        "football.team_rival",
                        "Sync SportMonks football team rival reference data."),
                    SportMonksApiRequest.Create("rivals")
                        .WithInclude(RivalReferenceIncludes),
                    cancellationToken: cancellationToken)).ToList();

                await _playerCoachSquadRivalWriter.UpsertTeamRivalsAsync(rivals, cancellationToken);
            }

            if (GetBoolean("SportMonksPlayerReferenceSync:SyncTeamSquads", false))
            {
                await ExecuteTeamSquads(teams, cancellationToken);
            }
        }

        private async Task ExecuteTeamSquads(
            IReadOnlyCollection<Team> teams,
            CancellationToken cancellationToken)
        {
            var maxTeamsPerRun = Math.Max(0, GetInteger("SportMonksPlayerReferenceSync:MaxSquadTeamsPerRun", 0));
            var teamList = teams
                .Where(team => team != null && team.Id > 0)
                .GroupBy(team => team.Id)
                .Select(group => group.Last())
                .OrderBy(team => team.Id)
                .ToList();

            if (maxTeamsPerRun > 0)
            {
                teamList = teamList.Take(maxTeamsPerRun).ToList();
            }

            foreach (var team in teamList)
            {
                var endpoint = $"squads/teams/{team.Id}";
                var teamSquads = (await _syncRunner.GetAllAsync<TeamSquad>(
                    SportMonksSyncJobDefinition.Create(
                        "sportmonks.football.team-squads.by-team",
                        "football.team_squad",
                        "Sync SportMonks football team squad rows by team."),
                    SportMonksApiRequest.Create(endpoint)
                        .WithInclude(TeamSquadIncludes)
                        .WithoutDefaultPagination(),
                    cursorKey: endpoint,
                    cancellationToken: cancellationToken)).ToList();

                await _playerCoachSquadRivalWriter.UpsertTeamSquadsAsync(teamSquads, cancellationToken);
            }
        }

        private async Task ExecuteStandingTopScorerReferences(
            IReadOnlyCollection<League> leagues,
            CancellationToken cancellationToken)
        {
            if (!GetBoolean("SportMonksStandingSync:Enabled", false))
            {
                return;
            }

            var seasons = GetStandingSyncSeasons(leagues);

            if (seasons.Count == 0)
            {
                _logger.LogWarning(
                    "SportMonks standings/top scorers sync is enabled, but no eligible seasons were found.");
                return;
            }

            if (GetBoolean("SportMonksStandingSync:SyncStandings", true))
            {
                foreach (var season in seasons)
                {
                    var endpoint = $"standings/seasons/{season.Id}";
                    var standings = (await _syncRunner.GetAllAsync<Standing>(
                        SportMonksSyncJobDefinition.Create(
                            "sportmonks.football.standings.by-season",
                            "competition.standing",
                            "Sync SportMonks football standings by season."),
                        SportMonksApiRequest.Create(endpoint)
                            .WithInclude(StandingIncludes)
                            .WithoutDefaultPagination(),
                        cursorKey: endpoint,
                        cancellationToken: cancellationToken)).ToList();

                    await _standingTopScorerWriter.UpsertStandingsAsync(standings, cancellationToken);
                }
            }

            if (GetBoolean("SportMonksStandingSync:SyncTopScorers", true))
            {
                foreach (var season in seasons)
                {
                    var endpoint = $"topscorers/seasons/{season.Id}";
                    var topScorers = (await _syncRunner.GetAllAsync<TopScorer>(
                        SportMonksSyncJobDefinition.Create(
                            "sportmonks.football.top-scorers.by-season",
                            "competition.top_scorer",
                            "Sync SportMonks football top scorers by season."),
                        SportMonksApiRequest.Create(endpoint)
                            .WithInclude(TopScorerIncludes),
                        cursorKey: endpoint,
                        cancellationToken: cancellationToken)).ToList();

                    await _standingTopScorerWriter.UpsertTopScorersAsync(topScorers, cancellationToken);
                }
            }
        }

        private IReadOnlyList<Season> GetStandingSyncSeasons(IEnumerable<League> leagues)
        {
            var currentOnly = GetBoolean("SportMonksStandingSync:CurrentSeasonsOnly", true);
            var maxSeasonsPerRun = Math.Max(0, GetInteger("SportMonksStandingSync:MaxSeasonsPerRun", 0));
            var seasons = ExtractStandingSyncSeasons(leagues)
                .Where(season => season != null && season.Id > 0)
                .Where(season => !currentOnly || season.IsCurrent)
                .GroupBy(season => season.Id)
                .Select(group => group.Last())
                .OrderByDescending(season => season.StartingAt ?? DateTime.MinValue)
                .ThenByDescending(season => season.Id)
                .ToList();

            if (maxSeasonsPerRun > 0)
            {
                seasons = seasons.Take(maxSeasonsPerRun).ToList();
            }

            return seasons;
        }

        private static IEnumerable<Season> ExtractStandingSyncSeasons(IEnumerable<League> leagues)
        {
            foreach (var league in leagues)
            {
                foreach (var season in league.Seasons ?? Enumerable.Empty<Season>())
                {
                    yield return season;
                }

                if (league.CurrentSeason != null)
                {
                    yield return league.CurrentSeason;
                }
            }
        }

        private async Task ExecuteTransferReferences(CancellationToken cancellationToken)
        {
            if (!GetBoolean("SportMonksTransferSidelinedSync:Enabled", false) ||
                !GetBoolean("SportMonksTransferSidelinedSync:SyncLatestTransfers", true))
            {
                return;
            }

            var order = NullIfWhiteSpace(_configuration["SportMonksTransferSidelinedSync:TransferOrder"]) ?? "desc";
            var transfers = (await _syncRunner.GetAllAsync<Transfer>(
                SportMonksSyncJobDefinition.Create(
                    "sportmonks.football.transfers.latest",
                    "football.transfer",
                    "Sync latest SportMonks football transfers."),
                SportMonksApiRequest.Create("transfers/latest")
                    .WithInclude(TransferIncludes)
                    .WithQueryParameter("order", order),
                cancellationToken: cancellationToken)).ToList();

            await _transferSidelinedWriter.UpsertTransfersAsync(transfers, cancellationToken);
        }

        private async Task ExecuteTvStationReferences(CancellationToken cancellationToken)
        {
            if (!GetBoolean("SportMonksFixtureMediaWeatherSync:Enabled", false) ||
                !GetBoolean("SportMonksFixtureMediaWeatherSync:SyncTvStations", true))
            {
                return;
            }

            var order = NullIfWhiteSpace(_configuration["SportMonksFixtureMediaWeatherSync:TvStationOrder"]) ?? "asc";
            var request = SportMonksApiRequest.Create("tv-stations")
                .WithQueryParameter("order", order);

            if (GetBoolean("SportMonksFixtureMediaWeatherSync:SyncTvStationCountries", true))
            {
                request.WithInclude(TvStationIncludes);
            }

            var tvStations = (await _syncRunner.GetAllAsync<TvStation>(
                SportMonksSyncJobDefinition.Create(
                    "sportmonks.football.tv-stations",
                    "football.tv_station",
                    "Sync SportMonks football TV station reference data."),
                request,
                cancellationToken: cancellationToken)).ToList();

            await _fixtureMediaWeatherWriter.UpsertTvStationsAsync(tvStations, cancellationToken);
        }

        private async Task ExecuteNewsReferences(CancellationToken cancellationToken)
        {
            if (!GetBoolean("SportMonksNewsSync:Enabled", false))
            {
                return;
            }

            if (GetBoolean("SportMonksNewsSync:SyncAllPreMatchNews", false))
            {
                await ExecuteNewsEndpoint(
                    "sportmonks.football.news.pre-match",
                    GetConfiguredEndpoint("SportMonksUrls:preMatchNews", "news/pre-match"),
                    "Sync SportMonks pre-match news.",
                    cancellationToken);
            }

            if (GetBoolean("SportMonksNewsSync:SyncUpcomingPreMatchNews", false))
            {
                await ExecuteNewsEndpoint(
                    "sportmonks.football.news.pre-match.upcoming",
                    GetConfiguredEndpoint("SportMonksUrls:preMatchNewsUpcoming", "news/pre-match/upcoming"),
                    "Sync SportMonks pre-match news for upcoming fixtures.",
                    cancellationToken);
            }

            if (GetBoolean("SportMonksNewsSync:SyncAllPostMatchNews", false))
            {
                await ExecuteNewsEndpoint(
                    "sportmonks.football.news.post-match",
                    GetConfiguredEndpoint("SportMonksUrls:postMatchNews", "news/post-match"),
                    "Sync SportMonks post-match news.",
                    cancellationToken);
            }
        }

        private async Task ExecuteNewsEndpoint(
            string jobKey,
            string endpoint,
            string description,
            CancellationToken cancellationToken)
        {
            var request = SportMonksApiRequest.Create(endpoint);
            var order = NullIfWhiteSpace(_configuration["SportMonksNewsSync:Order"]);

            if (order != null)
            {
                request.WithQueryParameter("order", order);
            }

            if (GetBoolean("SportMonksNewsSync:IncludeLines", true))
            {
                request.WithInclude(NewsIncludes);
            }

            var news = (await _syncRunner.GetAllAsync<News>(
                SportMonksSyncJobDefinition.Create(
                    jobKey,
                    "football.news",
                    description),
                request,
                cursorKey: endpoint,
                cancellationToken: cancellationToken)).ToList();

            await _newsWriter.UpsertNewsAsync(news, cancellationToken);
        }

        private async Task ExecuteFixtureWindow(CancellationToken cancellationToken)
        {
            if (!GetBoolean("SportMonksFixtureSync:Enabled", false))
            {
                return;
            }

            var daysBack = Math.Max(0, GetInteger("SportMonksFixtureSync:DaysBack", 0));
            var daysForward = Math.Max(0, GetInteger("SportMonksFixtureSync:DaysForward", 7));
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var fromDate = today.AddDays(-daysBack);
            var toDate = today.AddDays(daysForward);

            _logger.LogInformation(
                "Fixture window sync started for dates {FromDate} through {ToDate}.",
                fromDate,
                toDate);

            for (var date = fromDate; date <= toDate; date = date.AddDays(1))
            {
                await ExecuteFixturesByDate(date, cancellationToken);
            }
        }

        private async Task ExecuteFixturesByDate(DateOnly date, CancellationToken cancellationToken)
        {
            var dateValue = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var endpoint = $"{GetFixtureByDateEndpoint().TrimEnd('/')}/{dateValue}";
            var request = SportMonksApiRequest.Create(endpoint)
                .WithInclude(BuildFixtureSyncIncludes().ToArray());
            var timezone = NullIfWhiteSpace(_configuration["SportMonksFixtureSync:Timezone"]);

            if (timezone != null)
            {
                request.WithTimezone(timezone);
            }

            var fixtures = (await _syncRunner.GetAllAsync<Fixture>(
                SportMonksSyncJobDefinition.Create(
                    "sportmonks.football.fixtures.by-date",
                    "football.fixture",
                    "Sync SportMonks football fixtures with participants, scores, and periods."),
                request,
                cursorKey: endpoint,
                cancellationToken: cancellationToken)).ToList();

            await _fixtureCoreWriter.UpsertFixturesAsync(fixtures, cancellationToken);
            await _fixtureRefereeWriter.UpsertFixtureRefereesAsync(fixtures, cancellationToken);
            await _fixtureEventStatisticWriter.UpsertEventsAndStatisticsAsync(fixtures, cancellationToken);
            await _fixtureLineupFormationWriter.UpsertLineupsAndFormationsAsync(fixtures, cancellationToken);

            if (ShouldSyncFixtureSidelined())
            {
                await _transferSidelinedWriter.UpsertFixtureSidelinedAsync(fixtures, cancellationToken);
            }

            if (ShouldSyncFixtureMediaWeather())
            {
                await _fixtureMediaWeatherWriter.UpsertFixtureMediaWeatherAsync(fixtures, cancellationToken);
            }

            if (ShouldSyncFixtureTimeline())
            {
                await _fixtureTrendCommentaryWriter.UpsertTrendsAndCommentariesAsync(fixtures, cancellationToken);
            }

            if (ShouldSyncFixtureNews())
            {
                await _newsWriter.UpsertFixtureNewsAsync(fixtures, cancellationToken);
            }
        }

        private IEnumerable<string> BuildFixtureSyncIncludes()
        {
            foreach (var include in FixtureSyncIncludes)
            {
                yield return include;
            }

            if (ShouldSyncFixtureSidelined())
            {
                foreach (var include in FixtureSidelinedIncludes)
                {
                    yield return include;
                }
            }

            foreach (var include in BuildFixtureMediaWeatherIncludes())
            {
                yield return include;
            }

            foreach (var include in BuildFixtureTimelineIncludes())
            {
                yield return include;
            }

            foreach (var include in BuildFixtureNewsIncludes())
            {
                yield return include;
            }
        }

        private bool ShouldSyncFixtureSidelined()
        {
            return GetBoolean("SportMonksTransferSidelinedSync:Enabled", false) &&
                   GetBoolean("SportMonksTransferSidelinedSync:SyncFixtureSidelined", false);
        }

        private IEnumerable<string> BuildFixtureMediaWeatherIncludes()
        {
            if (!GetBoolean("SportMonksFixtureMediaWeatherSync:Enabled", false))
            {
                yield break;
            }

            if (GetBoolean("SportMonksFixtureMediaWeatherSync:SyncFixtureTvStations", false))
            {
                yield return "tvStations";
            }

            if (GetBoolean("SportMonksFixtureMediaWeatherSync:SyncWeatherReports", false))
            {
                yield return "weatherReport";
            }
        }

        private bool ShouldSyncFixtureMediaWeather()
        {
            return GetBoolean("SportMonksFixtureMediaWeatherSync:Enabled", false) &&
                   (GetBoolean("SportMonksFixtureMediaWeatherSync:SyncFixtureTvStations", false) ||
                    GetBoolean("SportMonksFixtureMediaWeatherSync:SyncWeatherReports", false));
        }

        private IEnumerable<string> BuildFixtureTimelineIncludes()
        {
            if (!GetBoolean("SportMonksFixtureTimelineSync:Enabled", false))
            {
                yield break;
            }

            if (GetBoolean("SportMonksFixtureTimelineSync:SyncTrends", false))
            {
                yield return "trends";
            }

            if (GetBoolean("SportMonksFixtureTimelineSync:SyncPressureTrends", false))
            {
                yield return "pressure";
            }

            if (GetBoolean("SportMonksFixtureTimelineSync:SyncCommentaries", false))
            {
                yield return "comments";
            }
        }

        private bool ShouldSyncFixtureTimeline()
        {
            return GetBoolean("SportMonksFixtureTimelineSync:Enabled", false) &&
                   (GetBoolean("SportMonksFixtureTimelineSync:SyncTrends", false) ||
                    GetBoolean("SportMonksFixtureTimelineSync:SyncPressureTrends", false) ||
                    GetBoolean("SportMonksFixtureTimelineSync:SyncCommentaries", false));
        }

        private IEnumerable<string> BuildFixtureNewsIncludes()
        {
            if (!GetBoolean("SportMonksNewsSync:Enabled", false))
            {
                yield break;
            }

            var includeLines = GetBoolean("SportMonksNewsSync:IncludeLines", true);

            if (GetBoolean("SportMonksNewsSync:SyncFixturePreMatchNews", false))
            {
                yield return "prematchNews";

                if (includeLines)
                {
                    yield return "prematchNews.lines";
                }
            }

            if (GetBoolean("SportMonksNewsSync:SyncFixturePostMatchNews", false))
            {
                yield return "postmatchNews";

                if (includeLines)
                {
                    yield return "postmatchNews.lines";
                }
            }
        }

        private bool ShouldSyncFixtureNews()
        {
            return GetBoolean("SportMonksNewsSync:Enabled", false) &&
                   (GetBoolean("SportMonksNewsSync:SyncFixturePreMatchNews", false) ||
                    GetBoolean("SportMonksNewsSync:SyncFixturePostMatchNews", false));
        }

        private string GetFixtureByDateEndpoint()
        {
            return NullIfWhiteSpace(_configuration["SportMonksUrls:fixtureByDate"]) ?? "fixtures/date/";
        }

        private string GetConfiguredEndpoint(string key, string defaultValue)
        {
            return NullIfWhiteSpace(_configuration[key]) ?? defaultValue;
        }

        private bool GetBoolean(string key, bool defaultValue)
        {
            var value = _configuration[key];

            if (bool.TryParse(value, out var result))
            {
                return result;
            }

            if (value == "1")
            {
                return true;
            }

            if (value == "0")
            {
                return false;
            }

            return defaultValue;
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

        private static string? NullIfWhiteSpace(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
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
