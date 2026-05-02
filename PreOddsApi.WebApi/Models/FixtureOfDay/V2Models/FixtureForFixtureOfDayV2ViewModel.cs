using PreOddsApi.WebApi.Models.Country.V2Models;
using PreOddsApi.WebApi.Models.League.V2Models;
using PreOddsApi.WebApi.Models.Team.V2Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.FixtureOfDay.V2Models
{
    public class FixtureForFixtureOfDayV2ViewModel
    {
        public long Id { get; set; }
        public LeagueV2ViewModel League { get; set; }
        public CountryV2ViewModel Country { get; set; }
        public TeamV2ViewModel LocalTeam { get; set; }
        public TeamV2ViewModel VisitorTeam { get; set; }
        public int LocalTeamScore { get; set; }
        public int VisitorTeamScore { get; set; }
        public int TimeMinute { get; set; }
        public string TimeStartingAtDateTime { get; set; }
        public string TimeStatus { get; set; }
        public string Status { get; set; }
        public List<OddForFixtureOfDayV2ViewModel> Odds { get; set; }
    }
}
