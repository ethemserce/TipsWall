using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities.FixtureOfDay
{
    public class FixtureForFixtureOfDayBusinessModel
    {
        public FixtureForFixtureOfDayBusinessModel()
        {
            this.Odds = new List<OddForFixtureOfDayBusinessModel>();
        }
        public long Id { get; set; }
        public LeagueBusinessModel League { get; set; }
        public CountryBusinessModel Country { get; set; }
        public TeamBusinessModel LocalTeam { get; set; }
        public TeamBusinessModel VisitorTeam { get; set; }
        public int LocalTeamScore { get; set; }
        public int VisitorTeamScore { get; set; }
        public int TimeMinute { get; set; }
        public string TimeStartingAtDateTime { get; set; }
        public string TimeStatus { get; set; }
        public string Status { get; set; }
        public List<OddForFixtureOfDayBusinessModel> Odds { get; set; }
    }
}
