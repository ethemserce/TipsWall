using PreOddsApi.BusinessLayer.Entities.BusinessEntities.User;
using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities.Tips
{
    public class TipsBusinessModel
    {
        public long Id { get; set; }
        public long OddId { get; set; }
        public long PrdUserId { get; set; }
        public long FixtureId { get; set; }
        public string UserName { get; set; }
        public string UserLogo { get; set; }
        public string OddValue { get; set; }
        public string OddLabel { get; set; }
        public string OddHandicap { get; set; }
        public string OddTotal { get; set; }
        public int IsWin { get; set; }
        public long MarketId { get; set; }
        public string MarketName { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public string LocalTeamName { get; set; }
        public string LocalTeamLogo { get; set; }
        public int LocalTeamScore { get; set; }
        public string VisitorTeamName { get; set; }
        public string VisitorTeamLogo { get; set; }
        public int VisitorTeamScore { get; set; }
        public long CountryId { get; set; }
        public string CountryName { get; set; }
        public string CountryLogo { get; set; }
        public long LeagueId { get; set; }
        public string LeagueName { get; set; }
        public string LeagueLogo { get; set; }
        public string HtScore { get; set; }
        public string TimeStatus { get; set; }
        public string TimeStartingAtDate { get; set; }
        public string TimeStartingAtDateTime { get; set; }
        public string TimeStartingAtTime { get; set; }
        public int TimeStartingAtTimestamp { get; set; }
    }
}
