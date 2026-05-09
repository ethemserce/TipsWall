using PreOddsApi.WebApi.Models.Team.V2Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Fixture.V2Models
{
    public class FixtureForRoundV2ViewModel
    {
        public long Id { get; set; }
        public string EtScore { get; set; }
        public string FtScore { get; set; }
        public string HtScore { get; set; }
        public TeamV2ViewModel LocalTeam { get; set; }
        public int LocalTeamPenScore { get; set; }
        public int LocalTeamScore { get; set; }
        public int TimeAddedTime { get; set; }
        public int TimeExtraMinute { get; set; }
        public int TimeInjuryTime { get; set; }
        public int TimeMinute { get; set; }
        public string TimeStartingAtDate { get; set; }
        public string TimeStartingAtDateTime { get; set; }
        public string TimeStartingAtTime { get; set; }
        public string TimeStatus { get; set; }
        public TeamV2ViewModel VisitorTeam { get; set; }
        public int VisitorTeamPenScore { get; set; }
        public int VisitorTeamScore { get; set; }
    }
}
