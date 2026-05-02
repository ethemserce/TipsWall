using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.Models.Tip.Models
{
    public class TipsViewModel
    {
        public long FixtureId { get; set; }
        public string UserName { get; set; }
        public string UserLogo { get; set; }
        public string OddValue { get; set; }
        public string OddLabel { get; set; }
        public int IsWin { get; set; }
        public string MarketName { get; set; }
        public string LocalTeamName { get; set; }
        public string LocalTeamLogo { get; set; }
        public int LocalTeamScore { get; set; }
        public string VisitorTeamName { get; set; }
        public string VisitorTeamLogo { get; set; }
        public int VisitorTeamScore { get; set; }
        public string CountryName { get; set; }
        public string CountryLogo { get; set; }
        public string LeagueName { get; set; }
        public string TimeStatus { get; set; }
        public string TimeStartingAtDateTime { get; set; }
        public string Status { get; set; }
    }
}
