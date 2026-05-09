using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Fixture
{
    public class FixtureForLiveViewModel
    {
        public FixtureForDateBaseViewModel FixtureForDate { get; set; }
        public List<FixtureForLeagueBaseViewModel> FixtureForLeague { get; set; }
        public List<FixtureForLeagueBaseViewModel> FixtureForLeagueLive { get; set; }
    }
}
