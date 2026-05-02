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

namespace PreOddsApi.WebApi.Controllers
{
    [Produces("application/json")]
    [Route("api")]
    public class StageController : Controller
    {
        private readonly IMapper _mapper;
        private readonly IStringLocalizer<StageController> _localizer;

        public StageController(IMapper mapper, IStringLocalizer<StageController> localizer)
        {
            _localizer = localizer;
            _mapper = mapper;
        }
    }
}