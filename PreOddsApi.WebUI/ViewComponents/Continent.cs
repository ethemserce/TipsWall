using Microsoft.AspNetCore.Mvc;
using PreOddsApi.WebUI.Models.Continent.Models;
using PreOddsApi.WebUI.PrdApiServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.ViewComponents
{
    public class Continent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            ContinentListViewModel continentModel = new ContinentListViewModel();//ContinentsApiService.GetContinents().Result;

            return View(continentModel);
        }
    }
}
