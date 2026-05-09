using PreOddsApi.WebApi.Models.Country.V2Models;
using PreOddsApi.WebApi.Models.League.V2Models;
using PreOddsApi.WebApi.Models.Odds.V2Models;
using PreOddsApi.WebApi.Models.Team.V2Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Fixture.V2Models.Analysis
{
    public class FixtureForOddAnalysisV2ViewModel
    {
        public long Id { get; set; }
        //public DateTime CreateDateTime { get; set; }
        public string EtScore { get; set; }
        public string FtScore { get; set; }
        //public string HtScore { get; set; }
        public long LeagueId { get; set; }
        public LeagueV2ViewModel League { get; set; }
        public CountryV2ViewModel Country { get; set; }
        public long LocalTeamId { get; set; }
        public TeamV2ViewModel LocalTeam { get; set; }
        //public int LocalTeamPenScore { get; set; }
        public int LocalTeamScore { get; set; }
        //public int TimeAddedTime { get; set; }
        //public int TimeExtraMinute { get; set; }
        //public int TimeInjuryTime { get; set; }
        //public int TimeMinute { get; set; }
        //public string TimeStartingAtDate { get; set; }
        public string TimeStartingAtDateTime { get; set; }
        //public string TimeStartingAtTime { get; set; }
        public int TimeStartingAtTimestamp { get; set; }
        public string TimeStartingAtTimezone { get; set; }
        public string TimeStatus { get; set; }
        public long VisitorTeamId { get; set; }
        public TeamV2ViewModel VisitorTeam { get; set; }
        //public int VisitorTeamPenScore { get; set; }
        public int VisitorTeamScore { get; set; }
        //public int? IddaaCode { get; set; }
        //public DateTime UpdateDateTime { get; set; }
        public List<OddV2ViewModel> Odds { get; set; }
    }
}
