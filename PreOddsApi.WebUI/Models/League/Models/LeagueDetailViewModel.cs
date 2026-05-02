using PreOddsApi.WebUI.Models.Season.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.Models.League.Models
{
    public class LeagueDetailViewModel
    {
        public List<SeasonViewModel> Seasons { get; set; }
    }
}
