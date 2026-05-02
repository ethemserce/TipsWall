using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using PreOddsApi.WebUI.Models.Tip.Models;
using PreOddsApi.WebUI.PrdApiServices;

namespace PreOddsApi.WebUI.Controllers
{
    public class TipsController : Controller
    {
        private readonly IConfiguration _configuration;
        public TipsController(IConfiguration configuration)
        {
            this._configuration = configuration;
        }

        public IActionResult Tips()
        {
            return View();
        }

        public JsonResult TipsJson(int page)
        {
            TipsBaseViewModel model = new TipsBaseViewModel();
            model = TipsApiServices.GetTips(HttpContext, page, _configuration["API_URL"]).Result; //JsonConvert.DeserializeObject<TipsBaseViewModel>(data);
            return Json(model);
        }
    }
}