using PreOddsApi.Core.Model;
using System;

namespace PreOddsApi.Entities.PreOddsEntities
{
    public class prd_coupon_item : BaseEntity
    {
        public long prd_coupon_id { get; set; }
        public long fixture_id { get; set; }
        public long bookmarker_id { get; set; }
        public long market_id { get; set; }
        public string odd_handicap { get; set; }
        public string odd_label { get; set; }
        public string odd_total { get; set; }
        public string odd_value { get; set; }
        public int? odd_winning { get; set; }
    }
}
