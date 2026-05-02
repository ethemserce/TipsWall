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
using PreOddsApi.WebApi.Models.Country;
using PreOddsApi.WebApi.Models.Continent.RequestModels;
using PreOddsApi.WebApi.Helpers;
using PreOddsApi.WebApi.Models.Continent.V2Models;
using PreOddsApi.WebApi.Models.Country.V2Models;
using PreOddsApi.WebApi.Models.League.V2Models;
using PreOddsApi.Entities.PreOddsEntities;

namespace PreOddsApi.WebApi.Controllers
{
    [Produces("application/json")]
    [Route("api")]
    public class ContinentController : Controller
    {
        private readonly IMapper _mapper;
        private readonly IContinentService _continentService;
        private readonly ILeagueService _leagueService;
        private readonly IStringLocalizer<ContinentController> _continentLocalizer;
        private readonly IStringLocalizer<CountryController> _countryLocalizer;

        public ContinentController(IContinentService continentService, ILeagueService leagueService, IMapper mapper,
            IStringLocalizer<ContinentController> continentLocalizer, IStringLocalizer<CountryController> countryLocalizer)
        {
            _continentService = continentService;
            _leagueService = leagueService;
            _continentLocalizer = continentLocalizer;
            _countryLocalizer = countryLocalizer;
            _mapper = mapper;
        }

        [HttpGet]
        [Route("continents&lang={lang}&apiKey={apiKey}")]
        public IActionResult GetContinents(string lang, string timeZone, string apiKey)
        {
            if (apiKey != "1")
            {
                return Json(null);
            }
            lang = ApiLanguageHandler.GetLanguage(lang);


            ContinentList model = new ContinentList();
            model.Continents = _mapper.Map<List<ContinentViewModel>>(_continentService.GetContinents());
            foreach (var continent in model.Continents)
            {
                continent.Name = _continentLocalizer[continent.Name.Trim().Replace(" ", "")].Value;

                foreach (var country in continent.Countries)
                {
                    SetCountryLang(country, lang);
                }
            }

            model.FavoriteLeagues = _mapper.Map<List<LeagueViewModel>>(_leagueService.GetFavoriteLeagues(lang));
            return Json(model);
        }

        [HttpPost]
        [Route("continents")]
        public IActionResult GetContinentsV2([FromBody] ContinentRequestBodyModel continentModel)
        {
            ContinentListV2ViewModel model = new ContinentListV2ViewModel();
            try
            {
                if (continentModel.ApiKey != ApiKeyHandler.GetApiKey())
                {
                    return Json(null);
                }
                continentModel.Language = ApiLanguageHandler.GetLanguage(continentModel.Language);

                ContinentList list = new ContinentList();
                list.Continents = _mapper.Map<List<ContinentViewModel>>(_continentService.GetContinents());

                foreach (var continent in list.Continents)
                {
                    continent.Name = _continentLocalizer[continent.Name.Trim().Replace(" ", "")].Value;

                    foreach (var country in continent.Countries)
                    {
                        SetCountryLang(country, continentModel.Language);
                    }

                    if (continent.Countries.Count > 0)
                    {
                        model.Continents.Add(_mapper.Map<ContinentV2ViewModel>(continent));
                    }
                }

                //model.FavoriteLeagues = _mapper.Map<List<LeagueV2ViewModel>>(_leagueService.GetFavoriteLeagues(continentModel.Language));
            }
            catch (Exception exc)
            {
                // Loglama yapılacak. LOGLAMA
                // model = null;
                return StatusCode(500, exc.Message);
            }

            return Json(model);
        }

        private void SetCountryLang(CountryViewModel model, string lang)
        {
            model.Name = _countryLocalizer[model.Name.Substring(0, 1) + model.Id];
            if (!string.IsNullOrEmpty(model.SubRegion))
            {
                model.SubRegion = _countryLocalizer[model.SubRegion.Trim().Replace(" ", "").Replace("-", "")].Value;
            }
            if (!string.IsNullOrEmpty(model.Continent))
            {
                model.Continent = _continentLocalizer[model.Continent.Trim().Replace(" ", "")].Value;
            }
        }
    }
}