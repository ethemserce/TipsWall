using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using PreOddsApi.WebUI.Models.Market.Models;
using PreOddsApi.WebUI.PrdApiServices;
using PreOddsApi.WebUI.Models.Analysis.Models;
using Microsoft.Extensions.Configuration;

namespace PreOddsApi.WebUI.Controllers
{
    public class AnalysisController : Controller
    {
        private readonly IConfiguration _configuration;
        public AnalysisController(IConfiguration configuration)
        {
            this._configuration = configuration;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Hotrates()
        {
            List<MarketViewModel> markets = MarketApiService.GetMarkets(HttpContext, _configuration["API_URL"]).Result;
            return View(markets);
        }

        public IActionResult WinningRates()
        {
            List<MarketViewModel> markets = MarketApiService.GetMarkets(HttpContext, _configuration["API_URL"]).Result;
            return View(markets);
        }

        public IActionResult EarningRates()
        {
            List<MarketViewModel> markets = MarketApiService.GetMarkets(HttpContext, _configuration["API_URL"]).Result;
            return View(markets);
        }


        public JsonResult HotratesJson(long marketId, string analysisPeriodId, int winningId, int earningId, string minRateId, int matchStateId, string date, int page)
        {
            DateTime hotrateDate;
            if (DateTime.TryParseExact(date, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out hotrateDate))
            {
                date = hotrateDate.Year + "-" + (hotrateDate.Month.ToString().Length == 1 ? "0" + hotrateDate.Month.ToString() : hotrateDate.Month.ToString()) + "-" + (hotrateDate.Day.ToString().Length == 1 ? "0" + hotrateDate.Day.ToString() : hotrateDate.Day.ToString());
            }
            else
            {
                date = DateTime.UtcNow.Year + "-" + (DateTime.UtcNow.Month.ToString().Length == 1 ? "0" + DateTime.UtcNow.Month.ToString() : DateTime.UtcNow.Month.ToString()) + "-" + (DateTime.UtcNow.Day.ToString().Length == 1 ? "0" + DateTime.UtcNow.Day.ToString() : DateTime.UtcNow.Day.ToString());
            }

            FixtureForOddAnalysisBaseViewModel model = AnalysisApiService.GetHotrates(HttpContext, 2, marketId, date, winningId, earningId, analysisPeriodId, minRateId, matchStateId, page, _configuration["API_URL"]).Result;
            return Json(model);
        }

        public JsonResult WinningJson(long marketId, string analysisPeriodId, int winningId, string minRateId, int matchStateId, string date, int page)
        {
            DateTime hotrateDate;
            if (DateTime.TryParseExact(date, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out hotrateDate))
            {
                date = hotrateDate.Year + "-" + (hotrateDate.Month.ToString().Length == 1 ? "0" + hotrateDate.Month.ToString() : hotrateDate.Month.ToString()) + "-" + (hotrateDate.Day.ToString().Length == 1 ? "0" + hotrateDate.Day.ToString() : hotrateDate.Day.ToString());
            }
            else
            {
                date = DateTime.UtcNow.Year + "-" + (DateTime.UtcNow.Month.ToString().Length == 1 ? "0" + DateTime.UtcNow.Month.ToString() : DateTime.UtcNow.Month.ToString()) + "-" + (DateTime.UtcNow.Day.ToString().Length == 1 ? "0" + DateTime.UtcNow.Day.ToString() : DateTime.UtcNow.Day.ToString());
            }

            FixtureForOddAnalysisBaseViewModel model = AnalysisApiService.GetWinning(HttpContext, 2, marketId, date, winningId, analysisPeriodId, minRateId, matchStateId, page, _configuration["API_URL"]).Result;
            return Json(model);
        }

        public JsonResult EarningJson(long marketId, string analysisPeriodId, string minRateId, int matchStateId, string date, int page)
        {
            DateTime hotrateDate;
            if (DateTime.TryParseExact(date, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out hotrateDate))
            {
                date = hotrateDate.Year + "-" + (hotrateDate.Month.ToString().Length == 1 ? "0" + hotrateDate.Month.ToString() : hotrateDate.Month.ToString()) + "-" + (hotrateDate.Day.ToString().Length == 1 ? "0" + hotrateDate.Day.ToString() : hotrateDate.Day.ToString());
            }
            else
            {
                date = DateTime.UtcNow.Year + "-" + (DateTime.UtcNow.Month.ToString().Length == 1 ? "0" + DateTime.UtcNow.Month.ToString() : DateTime.UtcNow.Month.ToString()) + "-" + (DateTime.UtcNow.Day.ToString().Length == 1 ? "0" + DateTime.UtcNow.Day.ToString() : DateTime.UtcNow.Day.ToString());
            }

            FixtureForOddAnalysisBaseViewModel model = AnalysisApiService.GetEarning(HttpContext, 2, marketId, date, analysisPeriodId, minRateId, matchStateId, page, _configuration["API_URL"]).Result;
            return Json(model);
        }
    }
}