using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities.Fixture
{
    public class FixtureForLeagueBaseBusinessModel
    {
        public FixtureForLeagueBaseBusinessModel()
        {
            this.Fixture = new List<FixtureForLeagueBusinessModel>();
        }

        public string TimeStartingAtDate { get; set; }
        public int MatchCount { get; set; }
        public int LiveMatchCount { get; set; }
        public LeagueBusinessModel League { get; set; }
        public CountryBusinessModel Country { get; set; }
        public GroupBusinessModel Group { get; set; }
        public List<FixtureForLeagueBusinessModel> Fixture { get; set; }
    }
}
