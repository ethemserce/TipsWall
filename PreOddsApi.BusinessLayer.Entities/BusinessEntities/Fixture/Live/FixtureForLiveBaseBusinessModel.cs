using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities.Fixture.Live
{
    public class FixtureForLiveBaseBusinessModel
    {
        public FixtureForLiveBaseBusinessModel()
        {
            this.FixtureForLeague = new List<FixtureForLiveLeagueBusinessModel>();
            this.FixtureForLeagueLive = new List<FixtureForLiveLeagueBusinessModel>();
        }
        public List<FixtureForLiveLeagueBusinessModel> FixtureForLeague { get; set; }
        public List<FixtureForLiveLeagueBusinessModel> FixtureForLeagueLive { get; set; }
    }
}
