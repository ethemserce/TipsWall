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
using PreOddsApi.WebApi.Helpers;
using PreOddsApi.WebApi.Models.Market.RequestModels;
using PreOddsApi.WebApi.Models.Market.V2Models;

namespace PreOddsApi.WebApi.Controllers
{
    [Produces("application/json")]
    [Route("api")]
    public class MarketController : Controller
    {
        private readonly IMapper _mapper;
        private readonly IMarketService _marketService;
        private readonly IStringLocalizer<MarketController> _marketLocalizer;

        public MarketController(IMarketService marketService, IMapper mapper, IStringLocalizer<MarketController> marketLocalizer)
        {
            _marketService = marketService;
            _marketLocalizer = marketLocalizer;
            _mapper = mapper;
        }

        [HttpGet]
        [Route("market&lang={lang}&apiKey={apiKey}")]
        public IActionResult GetMarkets(string lang, string apiKey)
        {
            if (apiKey != "1")
            {
                return Json(null);
            }
            lang = ApiLanguageHandler.GetLanguage(lang);

            var model = _mapper.Map<List<MarketViewModel>>(_marketService.GetMarkets());
            foreach (var market in model)
            {
                market.Name = SetMarketLang("market" + market.Id, lang);
            }

            return Json(model);
        }

        [HttpPost]
        [Route("markets")]
        public IActionResult GetMarketsV2([FromBody] MarketRequestBodyModel marketModel)
        {
            List<MarketV2ViewModel> data = null;
            try
            {
                if (marketModel.ApiKey != ApiKeyHandler.GetApiKey())
                {
                    return Json(null);
                }
                marketModel.Language = ApiLanguageHandler.GetLanguage(marketModel.Language);

                data = new List<MarketV2ViewModel>();
                var model = _mapper.Map<List<MarketViewModel>>(_marketService.GetMarkets());
                foreach (var market in model)
                {
                    // market.Name = SetMarketLang("market" + market.Id, marketModel.Language);
                    data.Add(_mapper.Map<MarketV2ViewModel>(market));
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

        private string SetMarketLang(string market, string lang)
        {
            return _marketLocalizer[market];
        }
    }
}