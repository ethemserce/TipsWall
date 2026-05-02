using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PreOddsApi.BusinessLayer.Abstract;
using AutoMapper;
using PreOddsApi.WebApi.Models.Continent;
using PreOddsApi.WebApi.Models.League;
using Microsoft.Extensions.Localization;
using System.Globalization;
using PreOddsApi.WebApi.Models.Fixture;
using Microsoft.AspNetCore.Cors;
using PreOddsApi.WebApi.Models.Tips;
using Microsoft.AspNetCore.Authorization;
using PreOddsApi.BusinessLayer.Entities.BusinessEntities.Tips;
using PreOddsApi.WebApi.Models.Tips.RequestModels;
using PreOddsApi.WebApi.Helpers;
using PreOddsApi.WebApi.Models.Tips.V2Models;
using PreOddsApi.WebApi.Models.FixtureOfDay.RequestModels;
using PreOddsApi.WebApi.Models.FixtureOfDay.V2Models;

namespace PreOddsApi.WebApi.Controllers
{
    [Produces("application/json")]
    [Route("api")]
    public class TipsController : Controller
    {
        private readonly IMapper _mapper;
        private readonly ITipsService _tipsService;
        private readonly IFixtureOfDayService _fixtureOfDayService;
        private readonly IStringLocalizer<TipsController> _tipsLocalizer;
        private readonly IStringLocalizer<CountryController> _countryLocalizer;

        public TipsController(ITipsService tipsService, IFixtureOfDayService fixtureOfDayService, IMapper mapper, IStringLocalizer<TipsController> tipsLocalizer, IStringLocalizer<CountryController> countryLocalizer)
        {
            _tipsService = tipsService;
            _tipsLocalizer = tipsLocalizer;
            _countryLocalizer = countryLocalizer;
            _fixtureOfDayService = fixtureOfDayService;
            _mapper = mapper;
        }

        [HttpGet]
        [Route("tips&page={page}&lang={lang}&timeZone={timeZone}&apiKey={apiKey}")]
        public IActionResult GetTips(int page, string lang, string timeZone, string apiKey)
        {
            if (apiKey != "1")
            {
                return Json(null);
            }

            lang = CheckLanguage(lang);
            TipsBaseViewModel model = new TipsBaseViewModel();
            model = _mapper.Map<TipsBaseViewModel>(_tipsService.GetTips(timeZone, page));

            foreach (var tip in model.Tips)
            {
                tip.MarketName = SetMarketLang("market" + tip.MarketId, lang);
                tip.OddLabel = SetOddLabelLang(tip.OddLabel, lang);
                tip.OddLabel = (tip.OddTotal + " " + tip.OddLabel + " " + tip.OddHandicap).Trim();
                tip.CountryName = GetCountryLang(tip.CountryName, tip.CountryId, lang);
                tip.OddValue = OddsHandler.FormatOdd(tip.OddValue);
            }
            return Json(model);
        }

        [HttpGet]
        [Route("tips2&page={page}&lang={lang}&timeZone={timeZone}&apiKey={apiKey}")]
        public IActionResult GetTips2(int page, string lang, string timeZone, string apiKey)
        {
            if (apiKey != "1")
            {
                return Json(null);
            }

            lang = CheckLanguage(lang);
            TipsBaseViewModel model = new TipsBaseViewModel();
            model = _mapper.Map<TipsBaseViewModel>(_tipsService.GetTips2(timeZone, page));

            foreach (var tip in model.Tips)
            {
                tip.MarketName = SetMarketLang("market" + tip.MarketId, lang);
                tip.OddLabel = SetOddLabelLang(tip.OddLabel, lang);
                tip.OddLabel = (tip.OddTotal + " " + tip.OddLabel + " " + tip.OddHandicap).Trim();
                tip.CountryName = GetCountryLang(tip.CountryName, tip.CountryId, lang);
            }
            return Json(model);
        }

        [Authorize]
        [HttpPost]
        [Route("insertTips")]
        public IActionResult InsertTips([FromBody]TipInsertViewModel request)
        {
            bool result = _tipsService.InsertTips(request.oddId, request.userId);
            return Json(new { result = result });
        }

        [HttpPost]
        [Route("tips")]
        public IActionResult GetTipsV2([FromBody] TipsRequestBodyModel tipsModel)
        {
            TipsBaseV2ViewModel data = null;
            try
            {
                if (tipsModel.ApiKey != ApiKeyHandler.GetApiKey())
                {
                    return Json(null);
                }

                tipsModel.Language = CheckLanguage(tipsModel.Language);
                TipsBaseViewModel model = new TipsBaseViewModel();
                model = _mapper.Map<TipsBaseViewModel>(_tipsService.GetTips(tipsModel.TimeZone, tipsModel.Page));

                data = new TipsBaseV2ViewModel();
                data.IsLastPage = model.IsLastPage;
                data.Page = model.Page;
                data.Success = model.Success;

                foreach (var tip in model.Tips)
                {
                    tip.MarketName = SetMarketLang("market" + tip.MarketId, tipsModel.Language);
                    tip.OddLabel = SetOddLabelLang(tip.OddLabel, tipsModel.Language);
                    tip.OddLabel = (tip.OddTotal + " " + tip.OddLabel + " " + tip.OddHandicap).Trim();
                    tip.OddValue = OddsHandler.FormatOdd(tip.OddValue);
                    tip.CountryName = GetCountryLang(tip.CountryName, tip.CountryId, tipsModel.Language);
                    tip.Status = GetStatus(tip.TimeStatus, tipsModel.Language);
                    data.Tips.Add(_mapper.Map<TipsV2ViewModel>(tip));
                }

            }
            catch (Exception exc)
            {
                // Loglama yapılacak. LOGLAMA
                //data = null;
                return StatusCode(500, exc.Message);
            }
            return Json(data);
        }

        [HttpPost]
        [Route("TopFixtureOfDay")]
        public IActionResult GetFixtureOfDay([FromBody] FixtureOfDayRequestBodyModel fixtureOfDayModel)
        {
            List<FixtureForFixtureOfDayV2ViewModel> model = null;
            try
            {

                if (fixtureOfDayModel.ApiKey != ApiKeyHandler.GetApiKey())
                {
                    return Json(null);
                }

                fixtureOfDayModel.Language = CheckLanguage(fixtureOfDayModel.Language);
                model = new List<FixtureForFixtureOfDayV2ViewModel>();
                var v = _fixtureOfDayService.GetFixtureOfDay(fixtureOfDayModel.Date, fixtureOfDayModel.TimeZone);
                var fixtureOfDayList = _mapper.Map<List<FixtureOfDayV2ViewModel>>(v);

                foreach (var fixtureOfDay in fixtureOfDayList)
                {
                    foreach (var odd in fixtureOfDay.Fixture.Odds)
                    {
                        odd.Market.Name = SetMarketLang("market" + odd.Market.Id, fixtureOfDayModel.Language);
                        odd.OddLabel = SetOddLabelLang(odd.OddLabel, fixtureOfDayModel.Language);
                        odd.OddValue = OddsHandler.FormatOdd(odd.OddValue);
                        //odd.OddLabel = (odd.OddTotal + " " + odd.OddLabel + " " + odd.OddHandicap).Trim();
                    }
                    fixtureOfDay.Fixture.Country.Name = GetCountryLang(fixtureOfDay.Fixture.Country.Name, fixtureOfDay.Fixture.Country.Id.ToString(), fixtureOfDayModel.Language);
                    fixtureOfDay.Fixture.Status = GetStatus(fixtureOfDay.Fixture.TimeStatus, fixtureOfDayModel.Language);
                    model.Add(fixtureOfDay.Fixture);
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

        private string SetMarketLang(string market, string lang)
        {
            return _tipsLocalizer[market.Trim()];
        }

        private string SetOddLabelLang(string label, string lang)
        {

            string oddLabelName = _tipsLocalizer["_" + label.Trim().Replace(" ", "_").Replace("/", "_")];
            return oddLabelName.Replace("_", "");
        }

        private string GetCountryLang(string countryName, string countryId, string lang)
        {
            return _countryLocalizer[countryName.Trim().Substring(0, 1) + countryId];
        }

        private string GetStatus(string timeStatus, string lang)
        {
            return _tipsLocalizer[timeStatus];
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
    }
}