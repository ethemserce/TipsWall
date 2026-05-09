using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Fixture.Live
{
    public class FixtureForLiveBaseViewModel
    {
        public List<FixtureForLiveLeagueViewModel> FixtureForLeague { get; set; }
        public List<FixtureForLiveLeagueViewModel> FixtureForLeagueLive { get; set; }
    }
}
