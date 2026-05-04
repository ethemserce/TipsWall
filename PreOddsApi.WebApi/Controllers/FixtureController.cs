using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PreOddsApi.BusinessLayer.Abstract;
using AutoMapper;
using PreOddsApi.WebApi.Models;
using PreOddsApi.BusinessLayer.Entities.BusinessEntities;
using PreOddsApi.WebApi.Models.Fixture;
using PreOddsApi.WebApi.Models.FixtureDetail;
using Microsoft.Extensions.Localization;
using System.Globalization;
using PreOddsApi.Entities;
using PreOddsApi.WebApi.Models.Country;
using PreOddsApi.WebApi.Models.Fixture.Live;
using Microsoft.AspNetCore.Cors;
using PreOddsApi.WebApi.Models.Fixture.RequestModels;
using PreOddsApi.WebApi.Helpers;
using PreOddsApi.WebApi.Models.Fixture.V2Models;
using PreOddsApi.WebApi.Models.Fixture.RequestModels.Analysis;
using PreOddsApi.WebApi.Models.Fixture.V2Models.Analysis;
using PreOddsApi.WebApi.Models.Country.V2Models;
using PreOddsApi.WebApi.Models.Odds.RequestModels;
using PreOddsApi.WebApi.Models.Market.V2Models;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace PreOddsApi.WebApi.Controllers
{
    [Produces("application/json")]
    [Route("api")]
    [DisableCors]
    public class FixtureController : Controller
    {
        private readonly IMapper _mapper;
        private readonly IFixtureService _fixtureService;
        private readonly IMarketService _marketService;
        private readonly IStringLocalizer<FixtureController> _fixtureLocalizer;
        private readonly IStringLocalizer<CountryController> _countryLocalizer;
        private readonly IStringLocalizer<ContinentController> _continentLocalizer;
        private readonly IStringLocalizer<MarketController> _marketLocalizer;
        private readonly IStringLocalizer<StageController> _stageLocalizer;
        private readonly ILogger<FixtureController> _logger;

        public FixtureController(IFixtureService fixtureService, IMarketService marketService,ILogger<FixtureController> logger,
            IStringLocalizer<FixtureController> fixtureLocalizer, IStringLocalizer<CountryController> countryLocalizer,
            IStringLocalizer<ContinentController> continentLocalizer, IStringLocalizer<MarketController> marketLocalizer, IStringLocalizer<StageController> stageLocalizer, IMapper mapper)
        {
            _fixtureService = fixtureService;
            _marketService = marketService;
            _fixtureLocalizer = fixtureLocalizer;
            _countryLocalizer = countryLocalizer;
            _continentLocalizer = continentLocalizer;
            _marketLocalizer = marketLocalizer;
            _stageLocalizer = stageLocalizer;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet]
        [Route("fixture/{fixtureId}&lang={lang}&timeZone={timeZone}&apiKey={apiKey}")]
        public IActionResult GetFixture(long fixtureId, string lang, string timeZone, string apiKey)
        {
            if (apiKey != "1")
            {
                return Json(null);
            }
            FixtureViewModel model = _mapper.Map<FixtureViewModel>(_fixtureService.GetFixture(fixtureId, timeZone));

            if (model.Continent != null)
            {
                model.Continent.Name = SetContinentLang(model.Continent.Name, lang);
            }

            if (model.Country != null)
            {
                SetCountryLang(model.Country, lang);
            }

            if (model.Stage != null)
            {
                model.Stage.Name = SetStageLang(model.Stage.Name, lang);
            }

            if (model.Group != null)
            {
                model.Group.Name = _fixtureLocalizer[model.Group.Name.Trim().Replace(" ", "_").Replace(":", "_").Replace(".", "_").Replace("-", "_")].Value;
            }

            if (model.LocalTeamCorner != null)
            {
                foreach (var corner in model.LocalTeamCorner)
                {
                    corner.Comment = _fixtureLocalizer[GetCornerNumber(corner.Comment.Replace(" ", ""))].Value;
                }
            }

            if (model.VisitorTeamCorner != null)
            {
                foreach (var corner in model.VisitorTeamCorner)
                {
                    corner.Comment = _fixtureLocalizer[GetCornerNumber(corner.Comment.Replace(" ", ""))].Value;
                }
            }

            if (!string.IsNullOrEmpty(model.WeatherType))
            {
                model.WeatherType = _fixtureLocalizer[model.WeatherType.Replace(" ", "")].Value;
            }

            if (!string.IsNullOrEmpty(model.WeatherCode))
            {
                model.WeatherCode = _fixtureLocalizer[model.WeatherCode.Replace(" ", "")].Value;
            }

            if (!string.IsNullOrEmpty(model.Pitch))
            {
                model.Pitch = _fixtureLocalizer[model.Pitch.Replace(" ", "").Replace("|", "")].Value;
            }

            return Json(model);
        }

        [HttpGet]
        [Route("fixtureOfDate/date={date}&ts={tarihSecim}&lang={lang}&timeZone={timeZone}&apiKey={apiKey}")]
        public IActionResult GetFixtureDate(string date, int tarihSecim, string lang, string timeZone, string apiKey)
        {
            if (apiKey != "1")
            {
                return Json(null);
            }


            double zone;
            if (!double.TryParse(timeZone, out zone))
            {
                return Json(null);
            }

            FixtureForLiveViewModel fixtureDate = _mapper.Map<FixtureForLiveViewModel>(_fixtureService.GetFixtureForLive(date, tarihSecim, timeZone));
            foreach (var fixture in fixtureDate.FixtureForDate.Fixture)
            {
                if (fixture.Country != null)
                {
                    SetCountryLang(fixture.Country, lang);
                }

            }

            foreach (var fixture in fixtureDate.FixtureForLeague)
            {
                if (fixture.Country != null)
                {
                    SetCountryLang(fixture.Country, lang);
                }
            }

            foreach (var fixture in fixtureDate.FixtureForLeagueLive)
            {
                if (fixture.Country != null)
                {
                    SetCountryLang(fixture.Country, lang);
                }
            }

            return Json(fixtureDate);
        }

        [HttpGet]
        [Route("fixtureLive/date={date}&ts={tarihSecim}&stt={statu}&lang={lang}&timeZone={timeZone}&apiKey={apiKey}")]
        public IActionResult GetFixtureLive(string date, int tarihSecim, int statu, string lang, string timeZone, string apiKey)
        {
            if (apiKey != "1")
            {
                return Json(null);
            }
            lang = CheckLanguage(lang);

            double zone;
            if (!double.TryParse(timeZone, out zone))
            {
                return Json(null);
            }

            FixtureForLiveViewModel fixtureDate = _mapper.Map<FixtureForLiveViewModel>(_fixtureService.GetFixtureForLiveV2(date, tarihSecim, statu, timeZone));
            //foreach (var item in fixtureDate.FixtureForDate)
            //{
            //    foreach (var fixture in item.Fixture)
            //    {
            //        if (fixture.Country != null)
            //        {
            //            SetCountryLang(fixture.Country, lang);
            //        }
            //    }
            //}

            foreach (var fixture in fixtureDate.FixtureForLeague)
            {
                if (fixture.Country != null)
                {
                    SetCountryLang(fixture.Country, lang);
                }
            }

            foreach (var fixture in fixtureDate.FixtureForLeagueLive)
            {
                if (fixture.Country != null)
                {
                    SetCountryLang(fixture.Country, lang);
                }
            }

            return Json(fixtureDate);
        }

        [HttpGet]
        [Route("favoritefixtures/id={fixtureIds}&lang={lang}&timeZone={timeZone}&apiKey={apiKey}")]
        public IActionResult GetFavoriteFixture(string fixtureIds, string lang, string timeZone, string apiKey)
        {
            if (apiKey != "1")
            {
                return Json(null);
            }
            lang = CheckLanguage(lang);


            double zone;
            if (!double.TryParse(timeZone, out zone))
            {
                return Json(null);
            }

            FixtureForFavoriteViewModel favoriteFixture = _mapper.Map<FixtureForFavoriteViewModel>(_fixtureService.GetFavoriteFixture(fixtureIds, timeZone));

            return Json(favoriteFixture);
        }

        [HttpGet]
        [Route("fixtureDetailHeader/{fixtureId}&lang={lang}&timeZone={timeZone}&apiKey={apiKey}")]
        public IActionResult GetFixtureDetailHeader(long fixtureId, string lang, string timeZone, string apiKey)
        {
            if (apiKey != "1")
            {
                return Json(null);
            }
            lang = CheckLanguage(lang);


            FixtureDetailHeaderViewModel model = _mapper.Map<FixtureDetailHeaderViewModel>(_fixtureService.GetFixtureDetailHeader(fixtureId, timeZone));

            return Json(model);
        }

        [HttpGet]
        [Route("fixtureOfRound/{leagueId}/{seasonId}/{stageId}/{groupId}/{roundId}&lang={lang}&timeZone={timeZone}&apiKey={apiKey}")]
        public IActionResult GetFixtureOfRound(long leagueId, long seasonId, long stageId, long groupId, long roundId, string lang, string timeZone, string apiKey)
        {
            if (apiKey != "1")
            {
                return Json(null);
            }

            lang = CheckLanguage(lang);

            List<FixtureForLeagueBaseViewModel> fixtures = _mapper.Map<List<FixtureForLeagueBaseViewModel>>(_fixtureService.GetFixtureByRoundId(leagueId, seasonId, stageId, groupId, roundId, timeZone));
            foreach (var fixture in fixtures)
            {
                if (fixture.Country != null)
                {
                    SetCountryLang(fixture.Country, lang);
                }
            }

            return Json(fixtures);
        }

        [HttpGet]
        [Route("fixtureOdds/{fixtureId}&lang={lang}&apiKey={apiKey}")]
        public IActionResult GetFixtureOdds(long fixtureId, string lang, string apiKey)
        {
            if (apiKey != "1")
            {
                return Json(null);
            }
            lang = CheckLanguage(lang);

            List<MarketViewModel> model = _mapper.Map<List<MarketViewModel>>(_marketService.GetMarkets(fixtureId));

            foreach (var market in model)
            {
                market.Name = SetMarketLang("market" + market.Id, lang);
            }
            return Json(model);
        }

        [HttpGet]
        [Route("fixtureOdds/{fixtureId}/{bookmakerId}&lang={lang}&apiKey={apiKey}")]
        public IActionResult GetFixtureOdds(long fixtureId, long bookmakerId, string lang, string apiKey)
        {
            if (apiKey != "1")
            {
                return Json(null);
            }
            lang = CheckLanguage(lang);

            bookmakerId = Getbookmaker_id(bookmakerId);

            List<MarketViewModel> model = _mapper.Map<List<MarketViewModel>>(_marketService.GetMarkets(fixtureId, bookmakerId));
            foreach (var market in model)
            {
                market.Name = SetMarketLang("market" + market.Id, lang);

                foreach (var bookmaker in market.bookmakers)
                {
                    foreach (var odd in bookmaker.Odd)
                    {
                        odd.OddLabel = _fixtureLocalizer["_" + odd.OddLabel.Trim().Replace(" ", "_").Replace("/", "_")].Value;
                    }
                }
            }

            return Json(model);
        }

        [HttpGet]
        [Route("fixtureOdds/{fixtureId}/{bookmakerId}/{marketId}&lang={lang}&apiKey={apiKey}")]
        public IActionResult GetFixtureOdds(long fixtureId, long bookmakerId, long marketId, string lang, string apiKey)
        {
            if (apiKey != "1")
            {
                return Json(null);
            }
            lang = CheckLanguage(lang);

            bookmakerId = Getbookmaker_id(bookmakerId);

            List<MarketViewModel> model = _mapper.Map<List<MarketViewModel>>(_marketService.GetMarkets(fixtureId, bookmakerId, marketId));
            foreach (var market in model)
            {
                market.Name = SetMarketLang("market" + market.Id, lang);

                foreach (var bookmaker in market.bookmakers)
                {
                    foreach (var odd in bookmaker.Odd)
                    {
                        odd.OddLabel = _fixtureLocalizer["_" + odd.OddLabel.Trim().Replace(" ", "_").Replace("/", "_")].Value;
                    }
                }
            }

            return Json(model);
        }

        [HttpGet]
        [Route("hotrateFixtures/{bookmaker_id}/{marketId}/date={date}&wp={winningPercent}&ep={earningPercente}&c={count}&minRate={rate}&af={allFixture}&page={page}&lang={lang}&timeZone={timeZone}&apiKey={apiKey}")]
        public IActionResult GetHotRateFixtures(long bookmaker_id, long marketId, string date, int winningPercent, int earningPercente, string count, string rate, int allFixture, int page, string lang, string timeZone, string apiKey)
        {
            if (apiKey != "1")
            {
                return Json(null);
            }
            lang = CheckLanguage(lang);

            bookmaker_id = Getbookmaker_id(bookmaker_id);

            DateTime datetime;
            if (!DateTime.TryParse(date, out datetime))
            {
                string result = "Tarih Formatı hatalı";
                return Json(result);
            }

            FixtureForOddAnalysisBaseViewModel model;
            do
            {
                model = _mapper.Map<FixtureForOddAnalysisBaseViewModel>(_fixtureService.GetHotRateFixtures(datetime.ToString("yyyy-MM-dd"), bookmaker_id, marketId, winningPercent, earningPercente, count, rate, allFixture, page, timeZone));
                page++;
            } while (model.Fixture.Count == 0 && model.IsLastPage == false && model.Success == true);

            foreach (var item in model.Fixture)
            {
                if (item.League != null)
                {
                    item.League.Seasons = null;
                    item.League.LogoSet = null;
                    SetCountryLang(item.League.Country, lang);
                }
                if (item.Country != null)
                {
                    SetCountryLang(item.Country, lang);
                }

                foreach (var odd in item.Odds)
                {
                    if (odd.Market != null)
                    {
                        odd.Market.bookmakers = null;
                        odd.Market.Name = SetMarketLang("market" + odd.Market.Id, lang);
                    }
                    odd.OddLabel = _fixtureLocalizer["_" + odd.OddLabel.Trim().Replace(" ", "_").Replace("/", "_")].Value;
                }
            }

            return Json(model);
        }

        [HttpGet]
        [Route("winningPercenteFixtures/{bookmaker_id}/{marketId}/date={date}&wp={winningPercent}&c={count}&minRate={rate}&af={allFixture}&page={page}&lang={lang}&timeZone={timeZone}&apiKey={apiKey}")]
        public IActionResult GetWinningPercenteFixtures(long bookmaker_id, long marketId, string date, int winningPercent, string count, string rate, int allFixture, int page, string lang, string timeZone, string apiKey)
        {
            if (apiKey != "1")
            {
                return Json(null);
            }
            lang = CheckLanguage(lang);

            DateTime datetime;
            if (!DateTime.TryParse(date, out datetime))
            {
                string result = "Tarih Formatı Hatalı";
                return Json(result);
            }

            bookmaker_id = Getbookmaker_id(bookmaker_id);

            FixtureForOddAnalysisBaseViewModel model;
            do
            {
                model = _mapper.Map<FixtureForOddAnalysisBaseViewModel>(_fixtureService.GetWinningPercenteFixtures(datetime.ToString("yyyy-MM-dd"), bookmaker_id, marketId, winningPercent, count, rate, allFixture, page, timeZone));
                page++;
            } while (model.Fixture.Count == 0 && model.IsLastPage == false && model.Success == true);

            foreach (var item in model.Fixture)
            {
                if (item.League != null)
                {
                    item.League.Seasons = null;
                    SetCountryLang(item.League.Country, lang);
                }
                if (item.Country != null)
                {
                    SetCountryLang(item.Country, lang);
                }

                foreach (var odd in item.Odds)
                {
                    if (odd.Market != null)
                    {
                        odd.Market.bookmakers = null;
                        odd.Market.Name = SetMarketLang("market" + odd.Market.Id, lang);
                    }
                    odd.OddLabel = _fixtureLocalizer["_" + odd.OddLabel.Trim().Replace(" ", "_").Replace("/", "_")].Value;
                }
            }

            return Json(model);
        }

        [HttpGet]
        [Route("earningPercenteFixtures/{bookmaker_id}/{marketId}/date={date}&c={count}&minRate={rate}&af={allFixture}&page={page}&lang={lang}&timeZone={timeZone}&apiKey={apiKey}")]
        public IActionResult GetEarningPercenteFixtures(long bookmaker_id, long marketId, string date, string count, string rate, int allFixture, int page, string lang, string timeZone, string apiKey)
        {
            if (apiKey != "1")
            {
                return Json(null);
            }
            lang = CheckLanguage(lang);

            DateTime datetime;
            if (!DateTime.TryParse(date, out datetime))
            {
                string result = "Tarih Formatı Hatalı";
                return Json(result);
            }

            bookmaker_id = Getbookmaker_id(bookmaker_id);

            FixtureForOddAnalysisBaseViewModel model;
            do
            {
                model = _mapper.Map<FixtureForOddAnalysisBaseViewModel>(_fixtureService.GetEarningPercenteFixtures(datetime.ToString("yyyy-MM-dd"), bookmaker_id, marketId, count, rate, allFixture, page, timeZone));
                page++;
            } while (model.Fixture.Count == 0 && model.IsLastPage == false && model.Success == true);

            foreach (var item in model.Fixture)
            {
                if (item.League != null)
                {
                    item.League.Seasons = null;
                    SetCountryLang(item.League.Country, lang);
                }
                if (item.Country != null)
                {
                    SetCountryLang(item.Country, lang);
                }

                foreach (var odd in item.Odds)
                {
                    if (odd.Market != null)
                    {
                        odd.Market.bookmakers = null;
                        odd.Market.Name = SetMarketLang("market" + odd.Market.Id, lang);
                    }
                    odd.OddLabel = _fixtureLocalizer["_" + odd.OddLabel.Trim().Replace(" ", "_").Replace("/", "_")].Value;
                }
            }

            return Json(model);
        }

        [HttpPost]
        [Route("fixtureOfDate")]
        [EnableCors("AllowSpecificOrigins")]
        public IActionResult GetFixtureDateV2([FromBody] FixtureOfDateRequestBodyModel fixtureOfDateModel)
        {
            FixtureForLiveV2ViewModel fixtureDate = null;
            try
            {
                if (fixtureOfDateModel.ApiKey != ApiKeyHandler.GetApiKey())
                {
                    return Json(null);
                }
                fixtureOfDateModel.Language = ApiLanguageHandler.GetLanguage(fixtureOfDateModel.Language);


                double zone;
                if (!double.TryParse(fixtureOfDateModel.TimeZone, out zone))
                {
                    return Json(null);
                }

                //FixtureForLiveV2ViewModel fixtureModelV2 = new FixtureForLiveV2ViewModel();
                fixtureDate = _mapper.Map<FixtureForLiveV2ViewModel>(_fixtureService.GetFixtureForLive(fixtureOfDateModel.Date, fixtureOfDateModel.TarihSecildimi, fixtureOfDateModel.TimeZone));
                //foreach (var fixture in fixtureDate.FixtureForDate.Fixture)
                //{
                //    if (fixture.Country != null)
                //    {
                //        SetCountryLang(fixture.Country, fixtureOfDateModel.Language);
                //    }

                //    if (fixture.TimeStatus == "LIVE")
                //    {
                //        fixture.ShortStatus = fixture.TimeMinute.ToString();
                //        if (fixture.TimeInjuryTime > 0)
                //        {
                //            fixture.ShortStatus += " + " + fixture.TimeInjuryTime.ToString();
                //        }
                //    }
                //    else if (fixture.TimeStatus == "ET")
                //    {
                //        fixture.ShortStatus = fixture.TimeExtraMinute.ToString();
                //    }
                //    else if (fixture.TimeStatus == "NS")
                //    {
                //        fixture.ShortStatus = fixture.TimeStartingAtTime;
                //    }
                //    else
                //    {
                //        fixture.Status = GetStatus(fixture.TimeStatus, fixtureOfDateModel.Language);
                //        fixture.ShortStatus = GetStatus(fixture.TimeStatus + "_Short", fixtureOfDateModel.Language);
                //    }
                //}

                foreach (var fixtureBase in fixtureDate.FixtureForLeague)
                {
                    if (fixtureBase.Country != null)
                    {
                        SetCountryLang(fixtureBase.Country, fixtureOfDateModel.Language);
                    }
                    foreach (var fixture in fixtureBase.Fixture)
                    {
                        if (fixture.TimeStatus == "LIVE")
                        {
                            fixture.ShortStatus = fixture.TimeMinute.ToString() + "'";
                            if (fixture.TimeInjuryTime > 0)
                            {
                                fixture.ShortStatus += " + " + fixture.TimeInjuryTime.ToString() + "'";
                            }
                        }
                        else if (fixture.TimeStatus == "ET")
                        {
                            fixture.ShortStatus = fixture.TimeExtraMinute.ToString() + "'";
                        }
                        else if (fixture.TimeStatus == "NS")
                        {
                            fixture.ShortStatus = fixture.TimeStartingAtTime.Substring(0, 5);
                        }
                        else
                        {
                            fixture.Status = GetStatus(fixture.TimeStatus, fixtureOfDateModel.Language);
                            fixture.ShortStatus = GetStatus(fixture.TimeStatus + "_Short", fixtureOfDateModel.Language);
                        }
                    }
                }

                foreach (var country in fixtureDate.Countries)
                {
                    if (country != null)
                    {
                        SetCountryLang(country, fixtureOfDateModel.Language);
                    }
                }

                //foreach (var fixtureBase in fixtureDate.FixtureForLeagueLive)
                //{
                //    if (fixtureBase.Country != null)
                //    {
                //        SetCountryLang(fixtureBase.Country, fixtureOfDateModel.Language);
                //    }
                //    foreach (var fixture in fixtureBase.Fixture)
                //    {
                //        if (fixture.TimeStatus == "LIVE")
                //        {
                //            fixture.ShortStatus = fixture.TimeMinute.ToString();
                //            if (fixture.TimeInjuryTime > 0)
                //            {
                //                fixture.ShortStatus += " + " + fixture.TimeInjuryTime.ToString();
                //            }
                //        }
                //        else if (fixture.TimeStatus == "ET")
                //        {
                //            fixture.ShortStatus = fixture.TimeExtraMinute.ToString();
                //        }
                //        else if (fixture.TimeStatus == "NS")
                //        {
                //            fixture.ShortStatus = fixture.TimeStartingAtTime;
                //        }
                //        else
                //        {
                //            fixture.Status = GetStatus(fixture.TimeStatus, fixtureOfDateModel.Language);
                //            fixture.ShortStatus = GetStatus(fixture.TimeStatus + "_Short", fixtureOfDateModel.Language);
                //        }
                //    }
                //}


            }
            catch (Exception exc)
            {
                // loglama yapılacak. LOGLAMA
                //fixtureDate = null;
                return StatusCode(500, exc.Message);
            }

            return Json(fixtureDate);
        }

        [HttpPost]
        [Route("fixture")]
        [EnableCors("AllowSpecificOrigins")]
        public IActionResult GetFixtureV2([FromBody] FixtureRequestBodyModel fixtureModel)
        {
            FixtureV2ViewModel model = new FixtureV2ViewModel();
            try
            {
                if (fixtureModel.ApiKey != ApiKeyHandler.GetApiKey())
                {
                    return Json(null);
                }
                fixtureModel.Language = ApiLanguageHandler.GetLanguage(fixtureModel.Language);

                model = _mapper.Map<FixtureV2ViewModel>(_fixtureService.GetFixture(fixtureModel.FixtureId, fixtureModel.TimeZone));

                if (model.Continent != null)
                {
                    model.Continent.Name = SetContinentLang(model.Continent.Name, fixtureModel.Language);
                }


                if (model.Country != null)
                {
                    SetCountryLang(model.Country, fixtureModel.Language);
                }


                if (model.Stage != null)
                {
                    model.Stage.Name = SetStageLang(model.Stage.Name, fixtureModel.Language);
                }


                if (model.Group != null)
                {
                    model.Group.Name = _fixtureLocalizer[model.Group.Name.Trim().Replace(" ", "_").Replace(":", "_").Replace(".", "_").Replace("-", "_")].Value;
                }


                if (model.LocalTeamCorner != null)
                {
                    foreach (var corner in model.LocalTeamCorner)
                    {
                        corner.Comment = _fixtureLocalizer[GetCornerNumber(corner.Comment.Replace(" ", ""))].Value;
                    }
                }

                if (model.VisitorTeamCorner != null)
                {
                    foreach (var corner in model.VisitorTeamCorner)
                    {
                        corner.Comment = _fixtureLocalizer[GetCornerNumber(corner.Comment.Replace(" ", ""))].Value;
                    }
                }

                if (!string.IsNullOrEmpty(model.WeatherType))
                {
                    model.WeatherType = _fixtureLocalizer[model.WeatherType.Replace(" ", "")].Value;
                }

                if (!string.IsNullOrEmpty(model.WeatherCode))
                {
                    model.WeatherCode = _fixtureLocalizer[model.WeatherCode.Replace(" ", "")].Value;
                }

                if (!string.IsNullOrEmpty(model.Pitch))
                {
                    model.Pitch = _fixtureLocalizer[model.Pitch.Replace(" ", "").Replace("|", "")].Value;
                }

                if (model.Venue != null)
                {
                    if (!string.IsNullOrEmpty(model.Venue.Surface))
                    {
                        model.Venue.Surface = _fixtureLocalizer[model.Venue.Surface].Value;
                    }
                }


                //if (fixtureModel.Language == "tr-TR")
                //{
                //    model.Comment = null;
                //}

                //    v2Model = _mapper.Map<FixtureV2ViewModel>(model);
                return Json(model);
            }
            catch (Exception exc)
            {
                //  loglama yapılacak. LOGLAMA
                //v2Model = null;
                return StatusCode(500, exc.Message);
            }
        }

        [HttpPost]
        [Route("favoriteFixtures")]
        [EnableCors("AllowSpecificOrigins")]
        public IActionResult GetFavoriteFixtureV2([FromBody] FavoriFixturesRequestBodyModel favoriFixturesModel)
        {
            FixtureForFavoriteV2ViewModel favoriteFixture = null;
            try
            {
                if (favoriFixturesModel.ApiKey != ApiKeyHandler.GetApiKey())
                {
                    return Json(null);
                }
                favoriFixturesModel.Language = ApiLanguageHandler.GetLanguage(favoriFixturesModel.Language);


                double zone;
                if (!double.TryParse(favoriFixturesModel.TimeZone, out zone))
                {
                    return Json(null);
                }

                favoriteFixture = _mapper.Map<FixtureForFavoriteV2ViewModel>(_fixtureService.GetFavoriteFixture(favoriFixturesModel.FixtureIds, favoriFixturesModel.TimeZone));

                foreach (var fixture in favoriteFixture.Fixture)
                {
                    if (fixture.TimeStatus == "LIVE")
                    {
                        fixture.ShortStatus = fixture.TimeMinute.ToString() + "'";
                        if (fixture.TimeInjuryTime > 0)
                        {
                            fixture.ShortStatus += " + " + fixture.TimeInjuryTime.ToString() + "'";
                        }
                    }
                    else if (fixture.TimeStatus == "ET")
                    {
                        fixture.ShortStatus = fixture.TimeExtraMinute.ToString() + "'";
                    }
                    else if (fixture.TimeStatus == "NS")
                    {
                        fixture.ShortStatus = fixture.TimeStartingAtTime.Substring(0, 5);
                    }
                    else
                    {
                        fixture.Status = GetStatus(fixture.TimeStatus, favoriFixturesModel.Language);
                        fixture.ShortStatus = GetStatus(fixture.TimeStatus + "_Short", favoriFixturesModel.Language);
                    }
                }
            }
            catch (Exception exc)
            {
                // Loglama yapılacak. LOGLAMA
                //favoriteFixture = null;
                return StatusCode(500, exc.Message);
            }
            return Json(favoriteFixture);
        }

        [HttpPost]
        [Route("fixtureOfRound")]
        [EnableCors("AllowSpecificOrigins")]
        public IActionResult GetFixtureOfRoundV2([FromBody] FixtureOfRoundRequestBodyModel fixtureOfRoundModel)
        {
            List<FixtureOfRoundBaseV2ViewModel> modelList = null;
            try
            {
                if (fixtureOfRoundModel.ApiKey != ApiKeyHandler.GetApiKey())
                {
                    return Json(null);
                }

                fixtureOfRoundModel.Language = ApiLanguageHandler.GetLanguage(fixtureOfRoundModel.Language);

                 modelList = new List<FixtureOfRoundBaseV2ViewModel>();
                List<FixtureForLeagueBaseViewModel> fixtures = _mapper.Map<List<FixtureForLeagueBaseViewModel>>(_fixtureService.GetFixtureByRoundId(fixtureOfRoundModel.LeagueId, fixtureOfRoundModel.SeasonId, fixtureOfRoundModel.StageId, fixtureOfRoundModel.GroupId, fixtureOfRoundModel.RoundId, fixtureOfRoundModel.TimeZone));
                foreach (var fixture in fixtures)
                {
                    if (fixture.Country != null)
                    {
                        SetCountryLang(fixture.Country, fixtureOfRoundModel.Language);
                    }

                    modelList.Add(_mapper.Map<FixtureOfRoundBaseV2ViewModel>(fixture));
                }
            }
            catch (Exception exc)
            {
                // loglama yapılacak. LOGLAMA
                //modelList = null;
                return StatusCode(500, exc.Message);
            }

            return Json(modelList);
        }

        [HttpPost]
        [Route("fixtureOdds")]
        [EnableCors("AllowSpecificOrigins")]
        public IActionResult GetFixtureOddsV2([FromBody] FixtureOddsRequestBodyModel fixtureOddsModel)
        {
            List<MarketForOddsV2ViewModel> modelV2 = null;
            try
            {
                if (fixtureOddsModel.ApiKey != ApiKeyHandler.GetApiKey())
                {
                    return Json(null);
                }

                fixtureOddsModel.Language = ApiLanguageHandler.GetLanguage(fixtureOddsModel.Language);

                fixtureOddsModel.bookmaker_id = Getbookmaker_id(fixtureOddsModel.bookmaker_id);

                 modelV2 = new List<MarketForOddsV2ViewModel>();
                List<MarketViewModel> model = _mapper.Map<List<MarketViewModel>>(_marketService.GetMarkets(fixtureOddsModel.FixtureId, fixtureOddsModel.bookmaker_id, fixtureOddsModel.MarketId));
                foreach (var market in model)
                {
                    market.Name = SetMarketLang("market" + market.Id, fixtureOddsModel.Language);

                    foreach (var bookmaker in market.bookmakers)
                    {
                        foreach (var odd in bookmaker.Odd)
                        {
                            odd.OddLabel = _fixtureLocalizer["_" + odd.OddLabel.Trim().Replace(" ", "_").Replace("/", "_")].Value;
                            odd.OddLabel = odd.OddLabel.Replace("_", "");
                            odd.OddValue = OddsHandler.FormatOdd(odd.OddValue);
                        }
                    }

                    modelV2.Add(_mapper.Map<MarketForOddsV2ViewModel>(market));
                }
            }
            catch (Exception exc)
            {
                // Loglama yapılacak. LOGLAMA
                //modelV2 = null;
                return StatusCode(500, exc.Message);
            }

            return Json(modelV2);
        }

        [HttpPost]
        [Route("hotrateFixtures")]
        [EnableCors("AllowSpecificOrigins")]
        public IActionResult GetHotRateFixturesV2([FromBody] HotRatesRequestBodyModel hotRatesModel)
        {
            FixtureForOddAnalysisBaseV2ViewModel model = new FixtureForOddAnalysisBaseV2ViewModel();
            try
            {
                if (hotRatesModel.ApiKey != ApiKeyHandler.GetApiKey())
                {
                    return Json(null);
                }
                hotRatesModel.Language = ApiLanguageHandler.GetLanguage(hotRatesModel.Language);

                hotRatesModel.BookmakerId = Getbookmaker_id(hotRatesModel.BookmakerId);

                DateTime datetime;
                if (!DateTime.TryParse(hotRatesModel.Date, out datetime))
                {
                    string result = "Tarih Formatı hatalı";
                    return Json(result);
                }

                do
                {
                    model = _mapper.Map<FixtureForOddAnalysisBaseV2ViewModel>(_fixtureService.GetHotRateFixtures(datetime.ToString("yyyy-MM-dd"), hotRatesModel.BookmakerId, hotRatesModel.MarketId, hotRatesModel.WinningPercente, hotRatesModel.EarningPercente, hotRatesModel.AnalysisPeriod, hotRatesModel.MinRate, hotRatesModel.MatchState, hotRatesModel.Page, hotRatesModel.Timezone));
                    hotRatesModel.Page++;
                } while (model.Fixture.Count == 0 && model.IsLastPage == false && model.Success == true);

                foreach (var item in model.Fixture)
                {
                    //if (item.League != null)
                    //{
                    //    //item.League.Seasons = null;
                    //    item.League.LogoSet = null;
                    //    //SetCountryLang(item.Country, hotRatesModel.Language);
                    //}
                    if (item.Country != null)
                    {
                        SetCountryLang(item.Country, hotRatesModel.Language);
                    }

                    foreach (var odd in item.Odds)
                    {
                        if (odd.Market != null)
                        {
                            //odd.Market.bookmakers = null;
                            odd.Market.Name = SetMarketLang("market" + odd.Market.Id, hotRatesModel.Language);
                        }
                        odd.OddLabel = _fixtureLocalizer["_" + odd.OddLabel.Trim().Replace(" ", "_").Replace("/", "_")].Value;
                        odd.OddValue = OddsHandler.FormatOdd(odd.OddValue);
                    }
                }
            }
            catch (Exception exc)
            {
                // Logalama yapılacak. LOGLAMA
                //model = null;
                _logger.LogError(exc, "hotrateFixtures post error");
                return StatusCode(500, exc.Message);
            }
            return Json(model);
        }

        [HttpPost]
        [Route("winningPercenteFixtures")]
        [EnableCors("AllowSpecificOrigins")]
        public IActionResult GetWinningPercenteFixturesV2([FromBody] WinningPercenteRequestBodyModel winningPercenteModel)
        {
            FixtureForOddAnalysisBaseV2ViewModel model;
            try
            {
                if (winningPercenteModel.ApiKey != ApiKeyHandler.GetApiKey())
                {
                    return Json(null);
                }
                winningPercenteModel.Language = ApiLanguageHandler.GetLanguage(winningPercenteModel.Language);

                DateTime datetime;
                if (!DateTime.TryParse(winningPercenteModel.Date, out datetime))
                {
                    string result = "Tarih Formatı Hatalı";
                    return Json(result);
                }

                winningPercenteModel.bookmaker_id = Getbookmaker_id(winningPercenteModel.bookmaker_id);

                do
                {
                    model = _mapper.Map<FixtureForOddAnalysisBaseV2ViewModel>(_fixtureService.GetWinningPercenteFixtures(datetime.ToString("yyyy-MM-dd"), winningPercenteModel.bookmaker_id, winningPercenteModel.MarketId, winningPercenteModel.WinningPercente, winningPercenteModel.AnalysisPeriod, winningPercenteModel.MinRate, winningPercenteModel.MatchState, winningPercenteModel.Page, winningPercenteModel.Timezone));
                    winningPercenteModel.Page++;
                } while (model.Fixture.Count == 0 && model.IsLastPage == false && model.Success == true);

                foreach (var item in model.Fixture)
                {
                    //if (item.League != null)
                    //{
                    //    item.League.Seasons = null;
                    //    SetCountryLang(item.League.Country, lang);
                    //}
                    if (item.Country != null)
                    {
                        SetCountryLang(item.Country, winningPercenteModel.Language);
                    }

                    foreach (var odd in item.Odds)
                    {
                        if (odd.Market != null)
                        {
                            //odd.Market.bookmakers = null;
                            odd.Market.Name = SetMarketLang("market" + odd.Market.Id, winningPercenteModel.Language);
                        }
                        odd.OddLabel = _fixtureLocalizer["_" + odd.OddLabel.Trim().Replace(" ", "_").Replace("/", "_")].Value;
                        odd.OddValue = OddsHandler.FormatOdd(odd.OddValue);
                    }
                }
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
        [Route("earningPercenteFixtures")]
        [EnableCors("AllowSpecificOrigins")]
        public IActionResult GetEarningPercenteFixturesV2([FromBody] EarningPercenteRequestBodyModel earningPercenteModel)
        {
            FixtureForOddAnalysisBaseV2ViewModel model;
            try
            {
                if (earningPercenteModel.ApiKey != ApiKeyHandler.GetApiKey())
                {
                    return Json(null);
                }
                earningPercenteModel.Language = ApiLanguageHandler.GetLanguage(earningPercenteModel.Language);

                DateTime datetime;
                if (!DateTime.TryParse(earningPercenteModel.Date, out datetime))
                {
                    string result = "Tarih Formatı Hatalı";
                    return Json(result);
                }

                earningPercenteModel.bookmaker_id = Getbookmaker_id(earningPercenteModel.bookmaker_id);

                do
                {
                    model = _mapper.Map<FixtureForOddAnalysisBaseV2ViewModel>(_fixtureService.GetEarningPercenteFixtures(datetime.ToString("yyyy-MM-dd"), earningPercenteModel.bookmaker_id, earningPercenteModel.MarketId, earningPercenteModel.AnalysisPeriod, earningPercenteModel.MinRate, earningPercenteModel.MatchState, earningPercenteModel.Page, earningPercenteModel.Timezone));
                    earningPercenteModel.Page++;
                } while (model.Fixture.Count == 0 && model.IsLastPage == false && model.Success == true);

                foreach (var item in model.Fixture)
                {
                    //if (item.League != null)
                    //{
                    //    item.League.Seasons = null;
                    //    SetCountryLang(item.League.Country, lang);
                    //}
                    if (item.Country != null)
                    {
                        SetCountryLang(item.Country, earningPercenteModel.Language);
                    }

                    foreach (var odd in item.Odds)
                    {
                        if (odd.Market != null)
                        {
                            //odd.Market.bookmakers = null;
                            odd.Market.Name = SetMarketLang("market" + odd.Market.Id, earningPercenteModel.Language);
                        }
                        odd.OddLabel = _fixtureLocalizer["_" + odd.OddLabel.Trim().Replace(" ", "_").Replace("/", "_")].Value;
                        odd.OddValue = OddsHandler.FormatOdd(odd.OddValue);
                    }
                }
            }
            catch (Exception exc)
            {
                // Loglama yapılacak. LOGLAMA
                //model = null;
                return StatusCode(500, exc.Message);
            }

            return Json(model);
        }

        private string GetCornerNumber(string name)
        {
            switch (name)
            {
                case "10thCorner": return "TenthCorner";
                case "11thCorner": return "EleventhCorner";
                case "12thCorner": return "TwelfthCorner";
                case "13thCorner": return "ThirteenthCorner";
                case "14thCorner": return "FourteenthCorner";
                case "15thCorner": return "FifteenthCorner";
                case "16thCorner": return "SixteenthCorner";
                case "17thCorner": return "SeventeenthCorner";
                case "18thCorner": return "EighteenthCorner";
                case "19thCorner": return "NineteenthCorner";
                case "1stCorner": return "FirstCorner";
                case "20thCorner": return "TwentiethCorner";
                case "21stCorner": return "TwentyFirstCorner";
                case "22ndCorner": return "TwentySecondCorner";
                case "2ndCorner": return "SecondCorner";
                case "3rdCorner": return "ThirdCorner";
                case "4thCorner": return "FourthCorner";
                case "5thCorner": return "FifthCorner";
                case "6thCorner": return "SixthCorner";
                case "7thCorner": return "SeventhCorner";
                case "8thCorner": return "EighthCorner";
                case "9thCorner": return "NinthCorner";
                default: return name;
            }
        }

        private long Getbookmaker_id(long bookmaker_id)
        {
            if (bookmaker_id == 2) // yurt dışı oranları için
            {
                return 2;
            }
            else if (bookmaker_id == 1) // iddaa oranları için
            {
                return 90100;
            }
            else
            {
                return 2;
            }
        }

        private void SetCountryLang(CountryViewModel model, string lang)
        {
            model.Name = _countryLocalizer[model.Name.Trim().Substring(0, 1) + model.Id];
            if (!string.IsNullOrEmpty(model.SubRegion))
            {
                model.SubRegion = _countryLocalizer[model.SubRegion.Trim().Replace(" ", "").Replace("-", "")].Value;
            }
            if (!string.IsNullOrEmpty(model.Continent))
            {
                model.Continent = SetContinentLang(model.Continent.Trim(), lang);
            }
        }

        private void SetCountryLang(CountryV2ViewModel model, string lang)
        {
            model.Name = _countryLocalizer[model.Name.Trim().Substring(0, 1) + model.Id].Value;
            //if (!string.IsNullOrEmpty(model.SubRegion))
            //{
            //    model.SubRegion = countryLocalizer.GetString(model.SubRegion.Trim().Replace(" ", "").Replace("-", "")).Value;
            //}
            //if (!string.IsNullOrEmpty(model.Continent))
            //{
            //    model.Continent = SetContinentLang(model.Continent.Trim(), lang);
            //}
        }

        private string GetCountryLang(string countryName, string countryId, string lang)
        {
            return _countryLocalizer[countryName.Trim().Substring(0, 1) + countryId].Value;
        }

        private string SetContinentLang(string continent, string lang)
        {
            if (!continent.IsNullOrEmpty())
            {
                return _continentLocalizer[continent.Trim().Replace(" ", "")].Value;
            }

            return "";
        }

        private string SetMarketLang(string market, string lang)
        {
            return _marketLocalizer[market.Trim()].Value;
        }

        private string SetStageLang(string stageName, string lang)
        {
            return _stageLocalizer["_" + stageName.Trim().Replace(" ", "_").Replace("/", "_").Replace("-", "_")].Value;
        }

        private string CheckLanguage(string lang)
        {
            switch (lang)
            {
                case "en-GB": return lang;
                case "en-US": return lang;
                case "tr-TR": return lang;
                default: return "en-GB";
            }
        }

        private string GetStatus(string timeStatus, string lang)
        {
            return _fixtureLocalizer[timeStatus].Value;
        }
    }
}