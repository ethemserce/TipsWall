using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PreOddsApi.WebUI.PrdApiServices;
using PreOddsApi.WebUI.Models.Fixture.Models;
using PreOddsApi.WebUI.Models.Events.Models;
using Microsoft.AspNetCore.Http;
using PreOddsApi.WebUI.Models.Market.Models;
using PreOddsApi.WebUI.Models.Odd.Models;
using PreOddsApi.WebUI.Models.Corner.Models;
using Microsoft.Extensions.Configuration;
using PreOddsApi.WebApi.Models.Fixture.V2Models;
using PreOddsApi.WebApi.Models.Market.V2Models;

namespace PreOddsApi.WebUI.Controllers
{
    public class FixtureController : Controller
    {
        private readonly IConfiguration _configuration;
        public FixtureController(IConfiguration configuration)
        {
            this._configuration = configuration;
        }
        public IActionResult FixtureDetail(long fixtureId)
        {
            List<MarketViewModel> markets = MarketApiService.GetMarkets(HttpContext, _configuration["API_URL"]).Result;
            ViewBag.fixtureId = fixtureId;
            return View(markets);
        }

        public JsonResult FixtureDetailJson(long fixtureId)
        {
            FixtureV2ViewModel model = FixtureApiService.GetFixture(HttpContext, fixtureId, _configuration["API_URL"]).Result;
            return Json(model);
        }

        public JsonResult FixtureOfDateJson(string date, int isDateSelected, string clientDate)
        {
            DateTime fixtureDate;
            DateTime clientDateTime;
            if (!string.IsNullOrEmpty(clientDate))
            {
                if (!DateTime.TryParse(clientDate, out clientDateTime))
                {
                    clientDateTime = DateTime.UtcNow;
                }
            }
            else
            {
                clientDateTime = DateTime.UtcNow;
            }

            if (!string.IsNullOrEmpty(date))
            {
                if (DateTime.TryParseExact(date, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out fixtureDate))
                {
                    date = fixtureDate.Year + "-" + (fixtureDate.Month.ToString().Length == 1 ? "0" + fixtureDate.Month.ToString() : fixtureDate.Month.ToString()) + "-" + (fixtureDate.Day.ToString().Length == 1 ? "0" + fixtureDate.Day.ToString() : fixtureDate.Day.ToString());
                }
                else
                {
                    date = DateTime.UtcNow.Year + "-" + (DateTime.UtcNow.Month.ToString().Length == 1 ? "0" + DateTime.UtcNow.Month.ToString() : DateTime.UtcNow.Month.ToString()) + "-" + (DateTime.UtcNow.Day.ToString().Length == 1 ? "0" + DateTime.UtcNow.Day.ToString() : DateTime.UtcNow.Day.ToString());
                    isDateSelected = 0;
                }
            }
            else
            {
                date = DateTime.UtcNow.Year + "-" + (DateTime.UtcNow.Month.ToString().Length == 1 ? "0" + DateTime.UtcNow.Month.ToString() : DateTime.UtcNow.Month.ToString()) + "-" + (DateTime.UtcNow.Day.ToString().Length == 1 ? "0" + DateTime.UtcNow.Day.ToString() : DateTime.UtcNow.Day.ToString());
                isDateSelected = 0;
            }

            //if (string.IsNullOrEmpty(date))
            //{
            //    date = clientDateTime.Year + "-" + (clientDateTime.Month.ToString().Length == 1 ? "0" + clientDateTime.Month.ToString() : clientDateTime.Month.ToString()) + "-" + (clientDateTime.Day.ToString().Length == 1 ? "0" + clientDateTime.Day.ToString() : clientDateTime.Day.ToString());
            //    isDateSelected = 0;
            //}
            //else
            //{
            //    if (DateTime.TryParse(date, out fixtureDate))
            //    {
            //        date = fixtureDate.Year + "-" + (fixtureDate.Month.ToString().Length == 1 ? "0" + fixtureDate.Month.ToString() : fixtureDate.Month.ToString()) + "-" + (fixtureDate.Day.ToString().Length == 1 ? "0" + fixtureDate.Day.ToString() : fixtureDate.Day.ToString());
            //    }
            //    else
            //    {
            //        date = DateTime.UtcNow.Year + "-" + (DateTime.UtcNow.Month.ToString().Length == 1 ? "0" + DateTime.UtcNow.Month.ToString() : DateTime.UtcNow.Month.ToString()) + "-" + (DateTime.UtcNow.Day.ToString().Length == 1 ? "0" + DateTime.UtcNow.Day.ToString() : DateTime.UtcNow.Day.ToString());
            //        isDateSelected = 0;
            //    }
            //}

            FixtureForLiveViewModel model = FixtureApiService.GetFixtureOfDate(HttpContext, date, isDateSelected, _configuration["API_URL"]).Result;

            if (model != null)
            {
                //var countryList = model.FixtureForLeague.GroupBy(p => p.Country.Id);
                //model.FixtureForDate.TimeStartingAtDate = date;
                if (date == (clientDateTime.Year + "-" + (clientDateTime.Month.ToString().Length == 1 ? "0" + clientDateTime.Month.ToString() : clientDateTime.Month.ToString()) + "-" + (clientDateTime.Day.ToString().Length == 1 ? "0" + clientDateTime.Day.ToString() : clientDateTime.Day.ToString())))
                {
                    model.RunTimer = true;
                }
                else
                {
                    model.RunTimer = false;
                }
            }
            ViewBag.LocalDate = clientDateTime;
            return Json(model);
        }

        public JsonResult FixtureOddsJson(long fixtureId, long marketId, int analysisPeriod)
        {
            MarketForOddsBaseViewModel model = new MarketForOddsBaseViewModel();
            List<MarketForOddsV2ViewModel> marketList = FixtureApiService.GetFixtureOdds(HttpContext, fixtureId, marketId, _configuration["API_URL"]).Result;

            if (marketList != null)
            {
                foreach (var item in marketList)
                {
                    item.SelectedAnalysisTypeId = analysisPeriod;
                }
            }

            model.Markets = marketList;
            return Json(model);
        }
    }
}