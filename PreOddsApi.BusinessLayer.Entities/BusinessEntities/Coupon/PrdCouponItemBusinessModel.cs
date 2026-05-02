using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities.Coupon
{
    public class PrdCouponItemBusinessModel
    {
        public long Id { get; set; }
        public long PrdCouponId { get; set; }
        public long FixtureId { get; set; }
        public long BookmarkerId { get; set; }
        public long MarketId { get; set; }
        public string MarketName { get; set; }
        public string OddHandicap { get; set; }
        public string OddLabel { get; set; }
        public string OddTotal { get; set; }
        public string OddValue { get; set; }
        public int? OddWinning { get; set; }
        public string TimeStartingAtDate { get; set; }
        public string TimeStartingAtTime { get; set; }
        public string LocalTeamName { get; set; }
        public string VisitorTeamName { get; set; }
        public int LocalTeamScore { get; set; }
        public int VisitorTeamScore { get; set; }
    }
}
