using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Coupon
{
    public class CouponItemViewModel
    {
        public long Id { get; set; }
        public long PrdCouponId { get; set; }
        public long FixtureId { get; set; }
        public string MarketName { get; set; }
        public string TimeStartingAtDate { get; set; }
        public string TimeStartingAtTime { get; set; }
        public string OddValue { get; set; }
        public string OddLabel { get; set; }
        public int? OddWinning { get; set; }
        public string LocalTeamName { get; set; }
        public string VisitorTeamName { get; set; }
    }
}
