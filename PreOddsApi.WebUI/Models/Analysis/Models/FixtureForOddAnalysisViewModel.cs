using PreOddsApi.WebUI.Models.Country.Models;
using PreOddsApi.WebUI.Models.League.Models;
using PreOddsApi.WebUI.Models.Odd.Models;
using PreOddsApi.WebUI.Models.Team.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.Models.Analysis.Models
{
    public class FixtureForOddAnalysisViewModel
    {
        public long Id { get; set; }
        public string EtScore { get; set; }
        public string FtScore { get; set; }
        public long LeagueId { get; set; }
        public LeagueViewModel League { get; set; }
        public CountryViewModel Country { get; set; }
        public long LocalTeamId { get; set; }
        public TeamViewModel LocalTeam { get; set; }
        public int LocalTeamScore { get; set; }
        public string TimeStartingAtDateTime { get; set; }
        public int TimeStartingAtTimestamp { get; set; }
        public string TimeStartingAtTimezone { get; set; }
        public string TimeStatus { get; set; }
        public long VisitorTeamId { get; set; }
        public TeamViewModel VisitorTeam { get; set; }
        public int VisitorTeamScore { get; set; }
        public List<OddViewModel> Odds { get; set; }
    }
}
