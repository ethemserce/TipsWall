using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using PreOddsApi.BusinessLayer.Abstract;
using AutoMapper;
using PreOddsApi.WebApi.Models.Country;
using Microsoft.Extensions.Localization;
using PreOddsApi.WebApi.Models.Country.RequestModels;
using PreOddsApi.WebApi.Helpers;
using PreOddsApi.WebApi.Models.Country.V2Models;
using PreOddsApi.Entities.PreOddsEntities;

namespace PreOddsApi.WebApi.Controllers
{
    [Produces("application/json")]
    [Route("api")]
    public class CountryController : Controller
    {
        private readonly IMapper _mapper;
        private readonly ICountryService _countryService;
        private readonly IStringLocalizer<CountryController> _countryLocalizer;
        private readonly IStringLocalizer<ContinentController> _continentLocalizer;

        public CountryController(ICountryService countryService, IMapper mapper,
             IStringLocalizer<CountryController> countryLocalizer, IStringLocalizer<ContinentController> continentLocalizer)
        {
            _countryService = countryService;
            _countryLocalizer = countryLocalizer;
            _continentLocalizer = continentLocalizer;
            _mapper = mapper;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [Route("countries&lang={lang}&apiKey={apiKey}")]
        public IActionResult GetCountries(string lang, string timeZone, string apiKey)
        {
            if (apiKey != "1")
            {
                return Json(null);
            }
            lang = ApiLanguageHandler.GetLanguage(lang);

            CountryList model = new CountryList();
            model.Countries = _mapper.Map<List<CountryViewModel>>(_countryService.GetCountries());
            foreach (var country in model.Countries)
            {
                SetCountryLang(country, lang);
            }
            return Json(model);
        }

        [HttpPost]
        [Route("countries")]
        public IActionResult GetCountriesV2([FromBody] CountryRequestBodyModel countryModel)
        {
            CountryListV2ViewModel data = new CountryListV2ViewModel();
            try
            {
                if (countryModel.ApiKey != ApiKeyHandler.GetApiKey())
                {
                    return Json(null);
                }
                countryModel.Language = ApiLanguageHandler.GetLanguage(countryModel.Language);


                CountryList model = new CountryList();
                model.Countries = _mapper.Map<List<CountryViewModel>>(_countryService.GetCountries());
                foreach (var country in model.Countries)
                {
                    SetCountryLang(country, countryModel.Language);
                    data.Countries.Add(_mapper.Map<CountryV2ViewModel>(country));
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

        private void SetCountryLang(CountryViewModel model, string lang)
        {
            model.Name = _countryLocalizer[model.Name.Substring(0, 1) + model.Id];
            if (!string.IsNullOrEmpty(model.SubRegion))
            {
                model.SubRegion = _countryLocalizer[model.SubRegion.Trim().Replace(" ", "").Replace("-", "")];
            }
            if (!string.IsNullOrEmpty(model.Continent))
            {
                model.Continent = _continentLocalizer[model.Continent.Trim().Replace(" ", "")];
            }
        }
    }
}