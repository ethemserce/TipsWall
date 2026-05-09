using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities.Fixture
{
    public class FixtureForDateBaseBusinessModel
    {
        public FixtureForDateBaseBusinessModel()
        {
            this.Fixture = new List<FixtureForLeagueBusinessModel>();
        }
        //public string TimeStartingAtDate { get; set; }
        public List<FixtureForLeagueBusinessModel> Fixture { get; set; }
    }
}
