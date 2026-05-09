using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities.Fixture
{
    public class FixtureForLiveBusinessModel
    {
        public FixtureForLiveBusinessModel()
        {
            this.FixtureForLeague = new List<FixtureForLeagueBaseBusinessModel>();
            this.FixtureForLeagueLive = new List<FixtureForLeagueBaseBusinessModel>();
            this.FixtureForDate = new FixtureForDateBaseBusinessModel();
            this.Countries = new List<CountryBusinessModel>();
        }
        public int MatchCount { get; set; }
        public int LiveMatchCount { get; set; }
        public FixtureForDateBaseBusinessModel FixtureForDate { get; set; }
        public List<FixtureForLeagueBaseBusinessModel> FixtureForLeague { get; set; }
        public List<FixtureForLeagueBaseBusinessModel> FixtureForLeagueLive { get; set; }
        public List<CountryBusinessModel> Countries { get; set; }
    }
}
