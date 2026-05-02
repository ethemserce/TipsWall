using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PreOddsApi.BusinessLayer.Abstract;
using AutoMapper;
using PreOddsApi.WebApi.Models;
using PreOddsApi.BusinessLayer.Entities.BusinessEntities;
using PreOddsApi.WebApi.Models.Country;
using PreOddsApi.WebApi.Models.Continent;
using PreOddsApi.WebApi.Models.League;
using PreOddsApi.WebApi.Models.Fixture;
using Microsoft.Extensions.Localization;
using System.Globalization;
using PreOddsApi.WebApi.Models.League.RequestModels;
using PreOddsApi.WebApi.Helpers;
using PreOddsApi.WebApi.Models.League.V2Models;
using PreOddsApi.WebApi.Models.TopScorer.V2Models;
using PreOddsApi.WebApi.Models.TopScorer.RequestModels;
using PreOddsApi.WebApi.Models.Statistic.RequestModels;

namespace PreOddsApi.WebApi.Controllers
{
    [Produces("application/json")]
    [Route("api")]
    public class LeagueController : Controller
    {
        private readonly IMapper _mapper;
        private readonly ILeagueService _leagueService;
        private readonly ISeasonService _seasonService;
        private readonly IStageService _stageService;
        private readonly IGroupService _groupService;
        private readonly IRoundService _roundService;
        private readonly IFixtureService _fixtureService;
        private readonly ITopScorerService _topScorerService;
        private readonly IStatisticService _statisticService;
        private readonly IStringLocalizer<LeagueController> _localizer;
        private readonly IStringLocalizer<StageController> _stageLocalizer;
        private readonly IStringLocalizer<CountryController> _countryLocalizer;
        private readonly IStringLocalizer<ContinentController> _continentLocalizer;

        public LeagueController(IStringLocalizer<LeagueController> localizer, ILeagueService leagueService, ISeasonService seasonService, IStringLocalizer<CountryController> countryLocalizer, IStringLocalizer<StageController> stageLocalizer,
             IStringLocalizer<ContinentController> continentLocalizer, IStageService stageService, IGroupService groupService, IRoundService roundService, IFixtureService fixtureService, ITopScorerService topScorerService,IStatisticService statisticService, IMapper mapper)
        {
            _leagueService = leagueService;
            _seasonService = seasonService;
            _stageService = stageService;
            _groupService = groupService;
            _roundService = roundService;
            _fixtureService = fixtureService;
            _topScorerService = topScorerService;
            _localizer = localizer;
            _stageLocalizer = stageLocalizer;
            _countryLocalizer = countryLocalizer;
            _continentLocalizer = continentLocalizer;
            _statisticService = statisticService;
            _mapper = mapper;
        }

        [HttpGet]
        [Route("leagues/{countryId}&lang={lang}&apiKey={apiKey}")]
        public IActionResult GetLeagues(long countryId, string lang, string timeZone, string apiKey)
        {
            if (apiKey != "1")
            {
                return Json(null);
            }

            lang = ApiLanguageHandler.GetLanguage(lang);

            LeagueList model = new LeagueList();
            model.Leagues = _mapper.Map<List<LeagueViewModel>>(_leagueService.GetLeagues(countryId, lang));

            return Json(model);
        }

        [HttpGet]
        [Route("topScorer/{leagueId}/{seasonId}/{stageId}&lang={lang}&apiKey={apiKey}")]
        public IActionResult GetTopScorer(long leagueId, long seasonId, long stageId, string lang, string apiKey)
        {
            if (apiKey != "1")
            {
                return Json(null);
            }
            lang = ApiLanguageHandler.GetLanguage(lang);


            TopScorerViewModel model = _mapper.Map<TopScorerViewModel>(_topScorerService.GetTopScorers(leagueId, seasonId, stageId, lang));

            return Json(model);
        }

        [HttpGet]
        [Route("leagueDetailInfo/{leagueId}&lang={lang}&apiKey={apiKey}")]
        public IActionResult Get(long leagueId, string lang, string apiKey)
        {
            try
            {
                if (apiKey != "1")
                {
                    return Json(null);
                }
                lang = ApiLanguageHandler.GetLanguage(lang);


                CultureInfo cultureInfo = new CultureInfo(lang);

                LeagueViewModel league = _mapper.Map<LeagueViewModel>(_leagueService.GetLeague(leagueId));
                if (league != null)
                {
                    SetCountryLang(league.Country, lang);

                    SeasonViewModel currentSeason = _mapper.Map<SeasonViewModel>(_seasonService.GetCurrentSeason(leagueId));
                    if (currentSeason != null)
                    {
                        league.Seasons.Add(currentSeason);
                    }
                    else
                    {
                        league.Seasons = _mapper.Map<List<SeasonViewModel>>(_seasonService.GetSeasons(leagueId));
                    }

                    foreach (var season in league.Seasons)
                    {
                        List<StageViewModel> stages = _mapper.Map<List<StageViewModel>>(_stageService.GetStages(season.Id));
                        if (stages != null && stages.Count > 0)
                        {
                            foreach (var stage in stages)
                            {
                                if (!season.CurrentSeason && season.CurrentStageId == 0)
                                {
                                    season.CurrentStageId = stage.Id;
                                }

                                stage.Name = SetStageLang(stage.Name, lang);
                                List<GroupViewModel> groups = _mapper.Map<List<GroupViewModel>>(_groupService.GetGroups(stage.Id));
                                if (groups.Count > 0)
                                {
                                    foreach (var group in groups)
                                    {
                                        group.Name = _localizer["_" + group.Name.Trim().Replace(" ", "_").Replace(":", "_").Replace(".", "_").Replace("-", "_")].Value;
                                        StageViewModel groupStage = new StageViewModel()
                                        {
                                            Id = stage.Id,
                                            CreateDateTime = stage.CreateDateTime,
                                            Name = group.Name,
                                            SeasonId = stage.SeasonId,
                                            Type = stage.Type,
                                            UpdateDateTime = stage.UpdateDateTime,
                                            GroupId = group.Id
                                        };

                                        //groupStage.Name = stage.Name + " " + group.Name;
                                        List<RoundViewModel> rounds = _mapper.Map<List<RoundViewModel>>(_roundService.GetRounds(stage.Id));
                                        if (rounds.Count > 0)
                                        {
                                            if (!season.CurrentSeason && season.CurrentRoundId == 0)
                                            {
                                                season.CurrentRoundId = rounds.FirstOrDefault().Id;
                                            }
                                            groupStage.Rounds = rounds;
                                            season.Stages.Add(groupStage);
                                        }
                                        else
                                        {
                                            groupStage.Rounds.Add(new RoundViewModel()
                                            {
                                                Id = 0,
                                                StageId = stage.Id,
                                                Name = 0,
                                            });
                                            season.Stages.Add(groupStage);
                                        }
                                    }
                                }
                                else
                                {
                                    List<RoundViewModel> rounds = _mapper.Map<List<RoundViewModel>>(_roundService.GetRounds(stage.Id));
                                    if (rounds.Count > 0)
                                    {
                                        if (!season.CurrentSeason && season.CurrentRoundId == 0)
                                        {
                                            season.CurrentRoundId = rounds.FirstOrDefault().Id;
                                        }
                                        stage.Rounds = rounds;
                                        season.Stages.Add(stage);
                                    }
                                    else
                                    {
                                        stage.Rounds.Add(new RoundViewModel()
                                        {
                                            Id = 0,
                                            StageId = stage.Id,
                                            Name = 0,
                                        });
                                        season.Stages.Add(stage);
                                    }
                                }
                            }
                        }
                        else
                        {
                            season.Stages.Add(new StageViewModel()
                            {
                                Id = 0,
                                Name = _localizer["RegularSeason"].Value,
                                SeasonId = season.Id,
                                Rounds = new List<RoundViewModel>()
                            {
                                new RoundViewModel()
                                {
                                    Id = 0,
                                    StageId = 0,
                                    Name = 0,
                                }
                            }
                            });
                        }
                    }
                }
                return Json(league);
            }
            catch (Exception exc)
            {

                return Json(exc.Message);
            }
        }

        [HttpPost]
        [Route("leagues")]
        public IActionResult GetLeaguesV2([FromBody] LeagueRequestBodyModel leagueModel)
        {
            LeagueListV2ViewModel model = null;
            try
            {
                if (leagueModel.ApiKey != ApiKeyHandler.GetApiKey())
                {
                    return Json(null);
                }

                leagueModel.Language = ApiLanguageHandler.GetLanguage(leagueModel.Language);

                model = new LeagueListV2ViewModel();
                model.Leagues = _mapper.Map<List<LeagueV2ViewModel>>(_leagueService.GetLeagues(leagueModel.CountryId, leagueModel.Language));

            }
            catch (Exception exc)
            {
                // Loglama yapılacak. LOGLAMA
                //model = null;
                return StatusCode(500, exc.Message);
            }
            return Json(model);
        }

        [HttpPost]
        [Route("topScorers")]
        public IActionResult GetTopScorerV2([FromBody] TopScorerRequestBodyModel topScorerModel)
        {
            TopScorerV2ViewModel model = null;
            try
            {
                if (topScorerModel.ApiKey != ApiKeyHandler.GetApiKey())
                {
                    return Json(null);
                }

                topScorerModel.Language = ApiLanguageHandler.GetLanguage(topScorerModel.Language);


                model = _mapper.Map<TopScorerV2ViewModel>(_topScorerService.GetTopScorers(topScorerModel.LeagueId, topScorerModel.SeasonId, topScorerModel.StageId, topScorerModel.Language));

            }
            catch (Exception exc)
            {
                // Loglama yapılacak. LOGLAMA
                //model = null;
                return StatusCode(500, exc.Message);
            }
            return Json(model);
        }

        [HttpPost]
        [Route("seasonStats")]
        public IActionResult GetSeasonStatsV2([FromBody] SeasonStatsRequestBodyModel seasonStatsModel)
        {
            SeasonStatsV2ViewModel model = null;
            try
            {
                if (seasonStatsModel.ApiKey != ApiKeyHandler.GetApiKey())
                {
                    return Json(null);
                }

                seasonStatsModel.Language = ApiLanguageHandler.GetLanguage(seasonStatsModel.Language);

                model = _mapper.Map<SeasonStatsV2ViewModel>(_statisticService.GetSeasonStats(seasonStatsModel.LeagueId, seasonStatsModel.SeasonId));

            }
            catch (Exception exc)
            {
                // Loglama yapılacak. LOGLAMA
                //model = null;
                return StatusCode(500, exc.Message);
            }
            return Json(model);
        }

        [HttpPost]
        [Route("leagueDetail")]
        public IActionResult LeagueDetailInfoV2([FromBody] LeagueDetailInfoRequestBodyModel leagueDetailInfoModel)
        {
            LeagueDetailV2ViewModel data = null;
            try
            {
                if (leagueDetailInfoModel.ApiKey != ApiKeyHandler.GetApiKey())
                {
                    return Json(null);
                }

                leagueDetailInfoModel.Language = ApiLanguageHandler.GetLanguage(leagueDetailInfoModel.Language);


                CultureInfo cultureInfo = new CultureInfo(leagueDetailInfoModel.Language);

                LeagueViewModel league = _mapper.Map<LeagueViewModel>(_leagueService.GetLeague(leagueDetailInfoModel.LeagueId));
                if (league != null)
                {
                    SetCountryLang(league.Country, leagueDetailInfoModel.Language);

                    SeasonViewModel currentSeason = _mapper.Map<SeasonViewModel>(_seasonService.GetCurrentSeason(leagueDetailInfoModel.LeagueId));
                    if (currentSeason != null)
                    {
                        league.Seasons.Add(currentSeason);
                    }
                    else
                    {
                        league.Seasons = _mapper.Map<List<SeasonViewModel>>(_seasonService.GetSeasons(leagueDetailInfoModel.LeagueId));
                    }

                    foreach (var season in league.Seasons)
                    {
                        List<StageViewModel> stages = _mapper.Map<List<StageViewModel>>(_stageService.GetStages(season.Id));
                        if (stages != null && stages.Count > 0)
                        {
                            foreach (var stage in stages)
                            {
                                if (!season.CurrentSeason && season.CurrentStageId == 0)
                                {
                                    season.CurrentStageId = stage.Id;
                                }

                                stage.Name = SetStageLang(stage.Name, leagueDetailInfoModel.Language);
                                List<GroupViewModel> groups = _mapper.Map<List<GroupViewModel>>(_groupService.GetGroups(stage.Id));
                                if (groups.Count > 0)
                                {
                                    foreach (var group in groups)
                                    {
                                        group.Name = _localizer["_" + group.Name.Trim().Replace(" ", "_").Replace(":", "_").Replace(".", "_").Replace("-", "_")].Value;
                                        StageViewModel groupStage = new StageViewModel()
                                        {
                                            Id = stage.Id,
                                            CreateDateTime = stage.CreateDateTime,
                                            Name = group.Name,
                                            SeasonId = stage.SeasonId,
                                            Type = stage.Type,
                                            UpdateDateTime = stage.UpdateDateTime,
                                            GroupId = group.Id
                                        };

                                        //groupStage.Name = stage.Name + " " + group.Name;
                                        List<RoundViewModel> rounds = _mapper.Map<List<RoundViewModel>>(_roundService.GetRounds(stage.Id));
                                        if (rounds.Count > 0)
                                        {
                                            if (!season.CurrentSeason && season.CurrentRoundId == 0)
                                            {
                                                season.CurrentRoundId = rounds.FirstOrDefault().Id;
                                            }
                                            groupStage.Rounds = rounds;
                                            season.Stages.Add(groupStage);
                                        }
                                        else
                                        {
                                            groupStage.Rounds.Add(new RoundViewModel()
                                            {
                                                Id = 0,
                                                StageId = stage.Id,
                                                Name = 0,
                                            });
                                            season.Stages.Add(groupStage);
                                        }
                                    }
                                }
                                else
                                {
                                    List<RoundViewModel> rounds = _mapper.Map<List<RoundViewModel>>(_roundService.GetRounds(stage.Id));
                                    if (rounds.Count > 0)
                                    {
                                        if (!season.CurrentSeason && season.CurrentRoundId == 0)
                                        {
                                            season.CurrentRoundId = rounds.FirstOrDefault().Id;
                                        }
                                        stage.Rounds = rounds;
                                        season.Stages.Add(stage);
                                    }
                                    else
                                    {
                                        stage.Rounds.Add(new RoundViewModel()
                                        {
                                            Id = 0,
                                            StageId = stage.Id,
                                            Name = 0,
                                        });
                                        season.Stages.Add(stage);
                                    }
                                }
                            }
                        }
                        else
                        {
                            season.Stages.Add(new StageViewModel()
                            {
                                Id = 0,
                                Name = _localizer["RegularSeason"].Value,
                                SeasonId = season.Id,
                                Rounds = new List<RoundViewModel>()
                            {
                                new RoundViewModel()
                                {
                                    Id = 0,
                                    StageId = 0,
                                    Name = 0,
                                }
                            }
                            });
                        }
                    }
                }

                data = _mapper.Map<LeagueDetailV2ViewModel>(league);

                return Json(league);
            }
            catch (Exception exc)
            {
                // Loglama yapılacak. LOGLAMA
                //data = null;
                return StatusCode(500, exc.Message);
            }

        }

        private string SetStageLang(string stageName, string lang)
        {
            return _stageLocalizer["_" + stageName.Trim().Replace(" ", "_").Replace("/", "_").Replace("-", "_")];
        }

        private void SetCountryLang(CountryViewModel model, string lang)
        {
            model.Name = _countryLocalizer[model.Name.Substring(0, 1) + model.Id].Value;
            if (!string.IsNullOrEmpty(model.SubRegion))
            {
                model.SubRegion = _countryLocalizer[model.SubRegion.Replace(" ", "").Replace("-", "")].Value;
            }
            if (!string.IsNullOrEmpty(model.Continent))
            {
                model.Continent = SetContinentLang(model.Continent, lang);
            }
        }

        private string SetContinentLang(string continent, string lang)
        {
            return _continentLocalizer[continent.Trim().Replace(" ", "")].Value;
        }
    }
}