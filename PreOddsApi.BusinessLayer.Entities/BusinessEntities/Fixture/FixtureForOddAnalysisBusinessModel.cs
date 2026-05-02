using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities.Fixture
{
    public class FixtureForOddAnalysisBusinessModel
    {
        public FixtureForOddAnalysisBusinessModel()
        {
            this.Odds = new List<OddBusinessModel>();
        }
        public long Id { get; set; }
        public DateTime CreateDateTime { get; set; }
        public string EtScore { get; set; }
        public string FtScore { get; set; }
        public string HtScore { get; set; }
        public long LeagueId { get; set; }
        public LeagueBusinessModel League { get; set; }
        public CountryBusinessModel Country { get; set; }
        public long LocalTeamId { get; set; }
        public TeamBusinessModel LocalTeam { get; set; }
        public int LocalTeamPenScore { get; set; }
        public int LocalTeamScore { get; set; }
        public int TimeAddedTime { get; set; }
        public int TimeExtraMinute { get; set; }
        public int TimeInjuryTime { get; set; }
        public int TimeMinute { get; set; }
        public string TimeStartingAtDate { get; set; }
        public string TimeStartingAtDateTime { get; set; }
        public string TimeStartingAtTime { get; set; }
        public int TimeStartingAtTimestamp { get; set; }
        public string TimeStartingAtTimezone { get; set; }
        public string TimeStatus { get; set; }
        public long VisitorTeamId { get; set; }
        public TeamBusinessModel VisitorTeam { get; set; }
        public int VisitorTeamPenScore { get; set; }
        public int VisitorTeamScore { get; set; }
        public DateTime UpdateDateTime { get; set; }
        public List<OddBusinessModel> Odds { get; set; }
        public int? IddaaCode { get; set; }
    }
}
