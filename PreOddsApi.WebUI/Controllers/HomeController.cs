using Microsoft.AspNetCore.Mvc;
using PreOddsApi.WebUI.Models;
using PreOddsApi.WebUI.Models.Continent.Models;
using PreOddsApi.WebUI.Models.Tip.Models;
using PreOddsApi.WebUI.PrdApiServices;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Web;
using Microsoft.AspNetCore.Http;
using PreOddsApi.WebUI.Helper;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace PreOddsApi.WebUI.Controllers
{
    public class HomeController : Controller
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;

        public HomeController(IWebHostEnvironment webHostEnvironment, IConfiguration configuration)
        {
            this._webHostEnvironment = webHostEnvironment;
            _configuration = configuration;
        }
        public IActionResult Index()
        {
            if (_webHostEnvironment.IsDevelopment())
            {

            }

            TipsBaseViewModel model = TipsApiServices.GetTips(HttpContext, 0, _configuration["API_URL"]).Result; //JsonConvert.DeserializeObject<TipsBaseViewModel>(data);
            if (model == null)
            {
                model = new TipsBaseViewModel();
            }
            model.FixtureOfDay = FixtureApiService.GetTopFixtureOfDay(HttpContext, DateTime.UtcNow.ToShortDateString(), _configuration["API_URL"]).Result;
            model.Continents = ContinentsApiService.GetContinents(HttpContext, _configuration["API_URL"]).Result;
            return View(model);
        }

        public void SetTimeZone(double timeZone)
        {
            var zone = TimeZoneInfo.GetSystemTimeZones().FirstOrDefault(x => x.BaseUtcOffset.TotalMinutes == -timeZone);
            HttpContext.Session.SetString("timezone", (-timeZone).ToString());
            //ViewBag.language = CultureHandler.GetLocalLanguage(HttpContext.Request);
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult PrivacyTerm()
        {
            return View();
        }

        public IActionResult Help()
        {
            return View();
        }

        public IActionResult AboutUs()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
