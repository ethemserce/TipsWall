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
using PreOddsApi.WebApi.Models.Odds;
using PreOddsApi.WebApi.Models.Odds.RequestModels;
using PreOddsApi.WebApi.Helpers;
using PreOddsApi.WebApi.Models.Odds.V2Models;

namespace PreOddsApi.WebApi.Controllers
{
    [Produces("application/json")]
    [Route("api")]
    public class OddController : Controller
    {
        private readonly IMapper _mapper;
        private readonly IOddService _oddService;

        public OddController(IOddService oddService, IMapper mapper)
        {
            _oddService = oddService;
            _mapper = mapper;
        }

        [HttpGet]
        [Route("oddSeries/{oddId}&lang={lang}&timeZone={timeZone}&apiKey={apiKey}")]
        public IActionResult GetOddSeries(long oddId, string lang, string timeZone, string apiKey)
        {
            if (apiKey != "1")
            {
                return Json(null);
            }
            lang = ApiLanguageHandler.GetLanguage(lang);

            var model = _mapper.Map<OddSeriesViewModel>(_oddService.GetOddSeries(oddId, timeZone));
            return Json(model);
        }

        [HttpPost]
        [Route("oddSeries")]
        public IActionResult GetOddSeriesV2([FromBody] OddSeriesRequestBodyModel oddModel)
        {
            OddSeriesV2ViewModel model = null;
            try
            {
                if (oddModel.ApiKey != ApiKeyHandler.GetApiKey())
                {
                    return Json(null);
                }

                oddModel.Language = ApiLanguageHandler.GetLanguage(oddModel.Language);

                model = _mapper.Map<OddSeriesV2ViewModel>(_oddService.GetOddSeries(oddModel.OddId, oddModel.TimeZone));

            }
            catch (Exception exc)
            {
                // Loglama yapılacak. LOGLAMA
                //model = null;
                return StatusCode(500, exc.Message);
            }
            return Json(model);
        }
    }
}