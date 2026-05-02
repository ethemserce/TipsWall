using PreOddsApi.WebApi.Models.Country.V2Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Fixture.V2Models
{
    public class FixtureForLiveV2ViewModel
    {
        public int MatchCount { get; set; }
        public int LiveMatchCount { get; set; }
        //public FixtureForDateBaseViewModel FixtureForDate { get; set; }
        public List<FixtureForLeagueBaseV2ViewModel> FixtureForLeague { get; set; } = new List<FixtureForLeagueBaseV2ViewModel>();
        //public List<FixtureForLeagueBaseV2ViewModel> FixtureForLeagueLive { get; set; }
        public List<CountryV2ViewModel> Countries { get; set; } = new List<CountryV2ViewModel>();
    }
}
