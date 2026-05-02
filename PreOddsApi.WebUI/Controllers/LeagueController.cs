using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PreOddsApi.WebUI.PrdApiServices;
using PreOddsApi.WebUI.Models.League.Models;
using PreOddsApi.WebUI.Models.Fixture.Models;
using PreOddsApi.WebUI.Helper;
using PreOddsApi.WebUI.Models.Season.Models;
using PreOddsApi.Utils;
using Microsoft.Extensions.Configuration;

namespace PreOddsApi.WebUI.Controllers
{
    public class LeagueController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string apiUrl;
        public LeagueController(IConfiguration configuration)
        {
            this._configuration = configuration;
            this.apiUrl = _configuration["API_URL"];

        }
        //private readonly ICacheHelper _cacheHelper;
        //public LeagueController(ICacheHelper cacheHelper)
        //{
        //    _cacheHelper = cacheHelper;
        //}

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Leagues()
        {
            return View();
        }

        public JsonResult ContinentJson()
        {
            var continents = ContinentsApiService.GetContinents(HttpContext, apiUrl).Result;
            return Json(continents);
        }

        public JsonResult LeaguesJson(long countryId)
        {
            var leagues = LeagueApiService.GetLeagues(HttpContext, countryId, apiUrl).Result;
            return Json(leagues);
        }

        public IActionResult LeagueDetail(long leagueId)
        {
            string apiUrl = _configuration["API_URL"];
            //ViewBag.language = CultureHandler.GetLocalLanguage(HttpContext.Request);

           // LeagueDetailBaseViewModel model = new LeagueDetailBaseViewModel();
            //model.LeagueDetail = (LeagueDetailViewModel)_cacheHelper.Get(string.Format(CacheKeys.LeagueDetail, leagueId));
            //if (model.LeagueDetail == null)
            //{
            //    model.LeagueDetail = LeagueApiService.GetLeagueDetail(HttpContext, leagueId).Result;
            //}

            LeagueDetailBaseViewModel model = LeagueApiService.GetLeagueDetail(HttpContext, leagueId, apiUrl).Result;

            if (model != null)
            {
                long seasonId = 0, stageId = 0, groupId = 0, roundId = 0;
                foreach (var season in model.Seasons)
                {
                    if (season.CurrentSeason)
                    {
                        var stage = season.Stages.FirstOrDefault(p => p.Id == season.CurrentStageId && p.GroupId == groupId);

                        if (stage != null)
                        {
                            seasonId = stage.SeasonId;
                            stageId = stage.Id;
                            groupId = stage.GroupId;
                        }
                        roundId = season.CurrentRoundId;
                        break;
                    }
                }
                //_cacheHelper.Set(string.Format(CacheKeys.LeagueDetail, leagueId), model, 45 * 60 * 60);


                model.TopScorers = TopScorerApiService.GetTopScorers(HttpContext, leagueId, seasonId, stageId, apiUrl).Result;
                model.FixtureOfRounds = FixtureApiService.GetFixtureOfRound(HttpContext, leagueId, seasonId, stageId, groupId, roundId, apiUrl).Result;
                model.LeagueStanding = StandingApiService.GetLeagueStanding(HttpContext, leagueId, seasonId, stageId, groupId, apiUrl).Result;
                model.SeasonStats = StatisticsApiService.GetSeasonStats(HttpContext, leagueId, seasonId, apiUrl).Result;
            }

            return View(model);
        }

        public async Task<JsonResult> FixtureOfRound(long leagueId, long seasonId, string stageId, long roundId)
        {
            //LeagueDetailViewModel leagueDetail = (LeagueDetailViewModel)_cacheHelper.Get(string.Format(CacheKeys.LeagueDetail, leagueId));

            //if (leagueDetail == null)
            //{
            //    leagueDetail = LeagueApiService.GetLeagueDetail(HttpContext, leagueId).Result;
            //}

            var leagueDetail = await LeagueApiService.GetLeagueDetail(HttpContext, leagueId, apiUrl);

            if (leagueDetail != null)
            {
                List<SeasonViewModel> seasons = leagueDetail.Seasons;
                SeasonViewModel season = seasons.FirstOrDefault(p => p.Id == seasonId);
                long groupId = long.Parse(stageId.Split('_')[1]);
                long cuurentStageId = long.Parse(stageId.Split('_')[0]);

                var stage = season.Stages.FirstOrDefault(p => p.Id == cuurentStageId && p.GroupId == groupId);

                if (stage != null)
                {
                    groupId = stage.GroupId;
                }


                List<FixtureOfRoundBaseViewModel> model = FixtureApiService.GetFixtureOfRound(HttpContext, leagueId, seasonId, cuurentStageId, groupId, roundId, apiUrl).Result;

                return Json(model);
            }

            return null;
        }

        public JsonResult LeagueDetailJson(long leagueId, long seasonId, string stageId)
        {
            //ViewBag.language = CultureHandler.GetLocalLanguage(HttpContext.Request);
            LeagueDetailBaseViewModel model = new LeagueDetailBaseViewModel();
            //model.LeagueDetail = (LeagueDetailViewModel)_cacheHelper.Get(string.Format(CacheKeys.LeagueDetail, leagueId));
            //if (model.LeagueDetail == null)
            //{
            //    model.LeagueDetail = LeagueApiService.GetLeagueDetail(HttpContext, leagueId).Result;
            //}

            model = LeagueApiService.GetLeagueDetail(HttpContext, leagueId, apiUrl).Result;
            long groupId = 0, roundId = 0;
            groupId = long.Parse(stageId.Split('_')[1]);
            long currentStageId = long.Parse(stageId.Split('_')[0]);
            foreach (var season in model.Seasons)
            {
                if (season.Id == seasonId)
                {
                    var stage = season.Stages.FirstOrDefault(p => p.Id == currentStageId && p.GroupId == groupId);
                    stage.CurrentStageId = stage.Id;
                    if (stage != null)
                    {
                        season.CurrentStageId = stage.Id;
                        if (season.CurrentSeason && currentStageId == season.CurrentStageId && season.CurrentRoundId != 0)
                        {
                            roundId = season.CurrentRoundId;

                        }
                        else
                        {
                            roundId = stage.Rounds.First().Id;
                        }

                        foreach (var round in stage.Rounds)
                        {
                            if (round.Id == roundId)
                            {
                                round.CurrentRoundId = roundId;
                            }
                        }
                        season.CurrentRoundId = roundId;
                    }
                    season.CurrentSeason = true;
                }
                else
                {
                    season.CurrentSeason = false;
                }
            }

            model.TopScorers = TopScorerApiService.GetTopScorers(HttpContext, leagueId, seasonId, currentStageId, apiUrl).Result;
            model.FixtureOfRounds = FixtureApiService.GetFixtureOfRound(HttpContext, leagueId, seasonId, currentStageId, groupId, roundId, apiUrl).Result;
            model.LeagueStanding = StandingApiService.GetLeagueStanding(HttpContext, leagueId, seasonId, currentStageId, groupId, apiUrl).Result;
            model.SeasonStats = StatisticsApiService.GetSeasonStats(HttpContext, leagueId, seasonId, apiUrl).Result;
            return Json(model);
        }
    }
}