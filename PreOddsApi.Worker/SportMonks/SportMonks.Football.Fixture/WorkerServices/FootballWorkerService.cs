using PreOddsApi.Core.Data.EntityFramework.Abstract;
using PreOddsApi.DataLayer;
using PreOddsApi.DataLayer.Mapping;
using PreOddsApi.Entities.PreOddsEntities;
using PreOddsApi.Entities.SportMonks.Core.Common.V3;
using PreOddsApi.Entities.SportMonks.Core.V3;
using PreOddsApi.Entities.SportMonks.Football;
using PreOddsApi.Entities.SportMonks.Football.Standings.V3;
using PreOddsApi.Entities.SportMonks.Football.Statistics.V3;
using PreOddsApi.Entities.SportMonks.Football.V3;
using PreOddsApi.Entities.SportMonks.Football.Weather.V3;
using PreOddsApi.Entities.SportMonks.Odds.V3;
using PreOddsApi.ExternalApis.SportMonks;

namespace SportMonks.Football.FixtureWorker.Services
{
    public class FootballWorkerService : BackgroundService
    {
        private readonly ILogger<FootballWorkerService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ISportMonksApiClient _sportMonksApiClient;
        private readonly IUpsertService<PreOddsApiDbContext> _upsertService;

        public FootballWorkerService(ILogger<FootballWorkerService> logger, IConfiguration configuration,
            ISportMonksApiClient sportMonksApiClient,
            IUpsertService<PreOddsApiDbContext> upsertService)
        {
            _logger = logger;
            _configuration = configuration;
            _sportMonksApiClient = sportMonksApiClient;
            _upsertService = upsertService;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Fixture Service execution started!");
                try
                {
                    await ExecuteLeague(stoppingToken);

                    //await ExecuteFixture(DateTime.Now.AddMonths(-12), DateTime.Now);
                    // TO DO: Fixture data insert to database
                }
                catch (Exception exc)
                {
                    _logger.LogError(exc, exc.Message);
                }
                await Task.Delay(100000, stoppingToken);
            }
        }

        private IEnumerable<DateTime> EachDay(DateTime from, DateTime thru)
        {
            for (var day = from.Date; day.Date <= thru.Date; day = day.AddDays(1))
                yield return day;
        }

        private async Task ExecuteLeague(CancellationToken cancellationToken)
        {
            var leagues = (await _sportMonksApiClient.GetAllAsync<League>(
                SportMonksApiRequest.Create("leagues")
                    .WithInclude("sport", "seasons", "stages", "stages.rounds", "seasons.groups"),
                cancellationToken)).ToList();

            await _upsertService.UpsertAsync<League, league>(leagues);
        }

        private async Task ExecuteStanding(CancellationToken cancellationToken)
        {
            var standings = (await _sportMonksApiClient.GetAllAsync<Standing>(
                SportMonksApiRequest.Create("standings")
                    .WithInclude("participant", "season", "league", "stage", "group", "round", "rule", "details", "details.type", "form", "sport"),
                cancellationToken)).ToList();

            await _upsertService.UpsertAsync<Standing, standing>(standings);

            foreach (var standing in standings)
            {
                //await _insertService.InsertAsync<Standing, standing>(standing);
                //await _insertService.InsertAsync<Participant, team>(standing.Participant);
                //await _insertService.InsertAsync<League, league>(standing.League);
                //await _insertService.InsertAsync<Season, season>(standing.Season);
                //await _insertService.InsertAsync<Stage, stage>(standing.Stage);
                //await _insertService.InsertAsync<Group, group>(standing.Group);
                //await _insertService.InsertAsync<Sport, sport>(standing.Sport);
                //await _insertService.InsertAsync<Round, round>(standing.Round);
                //if (standing.StandingDetail != null)
                //{
                //    foreach (var standingDetail in standing.StandingDetail)
                //    {
                //        await _insertService.InsertAsync<StandingDetail, standing_detail>(standingDetail);
                //        await _insertService.InsertAsync<Types, types>(standingDetail.Type);
                //    }
                //}

                //if (standing.StandingRule != null)
                //{
                //    await _insertService.InsertAsync<StandingRule, standing_rule>(standing.StandingRule);
                //    await _insertService.InsertAsync<Types, types>(standing.StandingRule.Type);
                //}

                //await _insertService.InsertAsync<StandingForm, standing_form>(standing.StandingForm);
            }
        }

        private async Task ExecuteFixture(DateTime from, DateTime to, CancellationToken cancellationToken)
        {
            foreach (var date in EachDay(from, to))
            {
                var fixtureByDateUrl = _configuration.GetSection("SportMonksUrls").GetValue<string>("fixtureByDate")
                    ?? "fixtures/date/";
                fixtureByDateUrl += date.ToString("yyyy-MM-dd");

                var fixtureList = (await _sportMonksApiClient.GetAllAsync<Fixture>(
                    SportMonksApiRequest.Create(fixtureByDateUrl)
                        .WithInclude("sport", "round", "stage", "group", "league", "season", "participants", "odds", "scores", "state", "events", "statistics", "aggregate")
                        .WithFilter("markets", "1,10,14,18,19,33,38,39,41,44,50,51")
                        .WithQueryParameter("sortBy", "starting_at"),
                    cancellationToken)).ToList();

                if (fixtureList != null && fixtureList.Count > 0)
                {
                    await _upsertService.UpsertAsync<Fixture, fixture>(fixtureList);
                    //foreach (var fixture in fixtureList)
                    //{
                    //await _upsertService.UpsertAsync<Fixture, fixture>(fixture);
                    //await _upsertService.UpsertAsync<Fixture, prd_fixture_of_day>(fixture);

                    //if (fixture.HasOdds)
                    //{
                    //    if (fixture.Odds != null && fixture.Odds.Count > 0)
                    //    {
                    //        try
                    //        {
                    //            foreach (var odd in fixture.Odds)
                    //            {
                    //                odd.OddGroupProbability = CalculateOddProbability(odd, fixture.Odds).ToString();
                    //            }
                    //            await _insertService.InsertAsync<PreMatchOdd, odd>(fixture.Odds);
                    //        }
                    //        catch (Exception exc)
                    //        {

                    //            throw;
                    //        }
                    //    }
                    //}

                    //await _insertService.InsertAsync<Participant, team>(fixture.Participants.FirstOrDefault(x => x.Meta.Location == PreOddsApi.Entities.SportMonks.Common.Enums.Location.Home));
                    //await _insertService.InsertAsync<Participant, team>(fixture.Participants.FirstOrDefault(x => x.Meta.Location == PreOddsApi.Entities.SportMonks.Common.Enums.Location.Away));
                    //await _insertService.InsertAsync<League, league>(fixture.League);
                    //await _insertService.InsertAsync<Sport, sport>(fixture.Sport);
                    //await _insertService.InsertAsync<Round, round>(fixture.Round);
                    //await _insertService.InsertAsync<Stage, stage>(fixture.Stage);
                    //await _insertService.InsertAsync<Group, group>(fixture.Group);
                    //await _insertService.InsertAsync<Aggregate, aggregate>(fixture.Aggregate);
                    //await _insertService.InsertAsync<Season, season>(fixture.Season);
                    //await _insertService.InsertAsync<Venue, venue>(fixture.Venue);
                    //await _insertService.InsertAsync<State, state>(fixture.State);
                    //await _insertService.InsertAsync<WeatherReport, weather_report>(fixture.WeatherReport);
                    //await _insertService.InsertAsync<Lineup, lineup>(fixture.Lineups);
                    //List<Player> players = new List<Player>();
                    //if (fixture.Lineups != null)
                    //{
                    //    foreach (var lineup in fixture.Lineups)
                    //    {
                    //        players.Add(lineup.Player);
                    //    }
                    //    await _insertService.InsertAsync<Player, player>(players);
                    //}
                    //await _insertService.InsertAsync<Event, events>(fixture.Events);
                    //await _insertService.InsertAsync<Event, events>(fixture.Timeline);
                    //await _insertService.InsertAsync<News, news>(fixture.Comments);
                    //await _insertService.InsertAsync<News, news>(fixture.PostmatchNews);
                    //await _insertService.InsertAsync<News, news>(fixture.PrematchNews);
                    //await _insertService.InsertAsync<Trend, trend>(fixture.Trends);
                    //await _insertService.InsertAsync<Statistic, statistic>(fixture.Statistics);
                    //await _insertService.InsertAsync<Period, period>(fixture.Periods);
                    //await _insertService.InsertAsync<TvStation, tvstation>(fixture.TvStations);
                    //await _insertService.InsertAsync<Referee, referee>(fixture.Referees);
                    //await _insertService.InsertAsync<Formation, formation>(fixture.Formations);
                    //await _insertService.InsertAsync<Sidelined, sidelined>(fixture.Sidelineds);
                    //await _insertService.InsertAsync<Score, score>(fixture.Scores);
                    //}
                }
            }
        }

        private decimal CalculateOddProbability(PreMatchOdd odd, List<PreMatchOdd> oddList)
        {
            if (odd.Value == "0")
            {
                return 0;
            }

            var odds = oddList.Where(p => p.BookmakerId == odd.BookmakerId && p.MarketId == odd.MarketId && p.Total == odd.Total && p.Handicap == odd.Handicap); //_unitOfWork.Repository<odd>().GetList(p => p.fixture_id == odd.FixtureId && p.bookmarker_id == odd.BookmarkerId && p.market_id == odd.MarketId && p.odd_total == odd.OddTotal && p.odd_handicap == odd.OddHandicap);

            decimal karPayi = 0;
            foreach (var item in odds)
            {
                if (item.Value == "0")
                {
                    continue;
                }
                else
                {
                    karPayi += Convert.ToDecimal(1 / decimal.Parse(item.Value));
                }
            }

            decimal dagitilacakTutar = 1 / karPayi;

            decimal dagitilacakTutarOrani = 100 * dagitilacakTutar;

            return Math.Round(Convert.ToDecimal(Convert.ToDecimal(1 / decimal.Parse(odd.Value)) * dagitilacakTutarOrani), 2);
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fixture Service started!");
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fixture Service stppped!");
            return base.StopAsync(cancellationToken);
        }
    }
}
