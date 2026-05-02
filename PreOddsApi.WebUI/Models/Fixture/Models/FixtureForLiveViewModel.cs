using PreOddsApi.WebUI.Models.Country.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.Models.Fixture.Models
{
    public class FixtureForLiveViewModel
    {
        public bool RunTimer { get; set; }
        public bool IsLive { get; set; }
        public bool CountrySelected { get; set; }
        public int MatchCount { get; set; }
        public int LiveMatchCount { get; set; }
        //public FixtureForDateBaseViewModel FixtureForDate { get; set; }
        public List<FixtureForLeagueBaseViewModel> FixtureForLeague { get; set; } = new List<FixtureForLeagueBaseViewModel>();
        //public List<FixtureForLeagueBaseViewModel> FixtureForLeagueLive { get; set; }
        public List<CountryViewModel> Countries { get; set; }= new List<CountryViewModel>();
    }
}
