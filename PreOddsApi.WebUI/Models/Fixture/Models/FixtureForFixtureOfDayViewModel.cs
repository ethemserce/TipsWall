using PreOddsApi.WebUI.Models.Country.Models;
using PreOddsApi.WebUI.Models.League.Models;
using PreOddsApi.WebUI.Models.Odd.Models;
using PreOddsApi.WebUI.Models.Team.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.Models.Fixture.Models
{
    public class FixtureForFixtureOfDayViewModel
    {
        public long Id { get; set; }
        public LeagueViewModel League { get; set; }
        public CountryViewModel Country { get; set; }
        public TeamViewModel LocalTeam { get; set; }
        public int LocalTeamScore { get; set; }
        public int TimeMinute { get; set; }
        public string TimeStartingAtDateTime { get; set; }
        public string TimeStatus { get; set; }
        public string Status { get; set; }
        public TeamViewModel VisitorTeam { get; set; }
        public int VisitorTeamScore { get; set; }
        public List<OddForFixtureOfDayViewModel> Odds { get; set; }
    }
}
