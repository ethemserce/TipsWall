using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PreOddsApi.WebUI.Models.Continent.Models;
using PreOddsApi.WebUI.PrdApiServices;

namespace PreOddsApi.WebUI.Controllers
{
    public class PartialsController : Controller
    {
        public IActionResult Continents()
        {
            ContinentListViewModel continentModel = ContinentsApiService.GetContinents().Result;

            return View(continentModel);
        }
    }
}