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
using Microsoft.AspNetCore.Cors;
using PreOddsApi.WebApi.Models.bookmaker.RequestModels;
using PreOddsApi.WebApi.Helpers;
using PreOddsApi.WebApi.Models.bookmaker.V2Models;
using PreOddsApi.BusinessLayer.Concrete;

namespace PreOddsApi.WebApi.Controllers
{
    [Produces("application/json")]
    [Route("api")]
    public class bookmakerController : Controller
    {
        private readonly IMapper _mapper;
        private readonly IBookmakerService _bookmakerService;
        private readonly IStringLocalizer<ContinentController> _localizer;

        public bookmakerController(IBookmakerService bookmakerService, IMapper mapper, IStringLocalizer<ContinentController> localizer)
        {
            _bookmakerService = bookmakerService;
            _localizer = localizer;
            _mapper = mapper;
        }

        [HttpGet]
        [Route("bookmaker&lang={lang}&apiKey={apiKey}")]
        public IActionResult Getbookmaker(string lang, string apiKey)
        {
            if (apiKey != "1")
            {
                return Json(null);
            }

            var model = _mapper.Map<List<bookmakerViewModel>>(_bookmakerService.GetBookmaker());
            return Json(model);
        }

        [HttpPost]
        [Route("bookmaker")]
        public IActionResult GetbookmakerV2([FromBody] bookmakerRequestBodyModel bookmakerModel)
        {
            List<bookmakerV2ViewModel> model = null;
            try
            {
                if (bookmakerModel.ApiKey != ApiKeyHandler.GetApiKey())
                {
                    return Json(null);
                }

                model = _mapper.Map<List<bookmakerV2ViewModel>>(_bookmakerService.GetBookmaker());
            }
            catch (Exception exc)
            {
                // Loglama Yapılacak. LOGLAMA
                return StatusCode(500, exc.Message);
            }
            return Json(model);
        }
    }
}