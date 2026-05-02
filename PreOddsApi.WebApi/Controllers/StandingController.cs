using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PreOddsApi.BusinessLayer.Abstract;
using AutoMapper;
using PreOddsApi.WebApi.Models.Continent;
using PreOddsApi.WebApi.Models.League;
using PreOddsApi.WebApi.Models.Fixture;
using PreOddsApi.WebApi.Models.Standing.RequestModels;
using PreOddsApi.WebApi.Helpers;
using PreOddsApi.WebApi.Models.Standing.V2Models;

namespace PreOddsApi.WebApi.Controllers
{
    [Produces("application/json")]
    [Route("api")]
    public class StandingController : Controller
    {
        private readonly IMapper _mapper;
        private readonly IStandingService _standingService;

        public StandingController(IStandingService standingService, IMapper mapper)
        {
            _standingService = standingService;
            _mapper = mapper;
        }

        [HttpGet]
        [Route("standing/{teamId}&lang={lang}&apiKey={apiKey}")]
        public IActionResult GetContinents(long teamId,string lang, string apiKey)
        {
            if (apiKey != "1")
            {
                return Json(null);
            }
            lang = ApiLanguageHandler.GetLanguage(lang);

            StandingViewModel model = _mapper.Map<StandingViewModel>(_standingService.GetStanding(teamId));
            return Json(model);
        }

        [HttpGet]
        [Route("standings/{leagueId}/{seasonId}/{stageId}/{groupId}&lang={lang}&apiKey={apiKey}")]
        public IActionResult GetContinents(long leagueId, long seasonId, long stageId, long groupId,string lang, string apiKey)
        {
            if (apiKey != "1")
            {
                return Json(null);
            }
            lang = ApiLanguageHandler.GetLanguage(lang);

            List<StandingViewModel> model = _mapper.Map<List<StandingViewModel>>(_standingService.GetStandings(leagueId, seasonId, stageId, groupId));
            return Json(model);
        }


        [HttpPost]
        [Route("teamStandings")]
        public IActionResult GetStandingsV2([FromBody] TeamStandingRequestBodyModel standingModel)
        {
            StandingV2ViewModel model = null;
            try
            {
                if (standingModel.ApiKey != ApiKeyHandler.GetApiKey())
                {
                    return Json(null);
                }
                standingModel.Language = ApiLanguageHandler.GetLanguage(standingModel.Language);

                model = _mapper.Map<StandingV2ViewModel>(_standingService.GetStanding(standingModel.TeamId));

            }
            catch (Exception exc)
            {
                // Loglama yapılacak. LOGLAMA
                //model = null;
                return StatusCode(500, exc.Message);
            }
            return Json(model);
        }

        [HttpPost]
        [Route("leagueStandings")]
        public IActionResult GetStandingsV2([FromBody] LeagueStandingRequestBodyModel standingModel)
        {
            List<StandingV2ViewModel> model = null;
            try
            {
                if (standingModel.ApiKey != ApiKeyHandler.GetApiKey())
                {
                    return Json(null);
                }
                standingModel.Language = ApiLanguageHandler.GetLanguage(standingModel.Language);

               model = _mapper.Map<List<StandingV2ViewModel>>(_standingService.GetStandings(standingModel.LeagueId, standingModel.SeasonId, standingModel.StageId, standingModel.GroupId));

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