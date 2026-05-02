using PreOddsApi.WebUI.Models.Country.Models;
using PreOddsApi.WebUI.Models.Group.Models;
using PreOddsApi.WebUI.Models.League.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.Models.Fixture.Models
{
    public class FixtureForLeagueBaseViewModel
    {
        public string TimeStartingAtDate { get; set; }
        public int MatchCount { get; set; }
        public int LiveMatchCount { get; set; }
        public LeagueViewModel League { get; set; }
        public CountryViewModel Country { get; set; }
        public GroupViewModel Group { get; set; }
        public List<FixtureForLeagueViewModel> Fixture { get; set; }
        public bool IsOpen { get; set; }
        public bool CountrySelected { get; set; }
    }
}
