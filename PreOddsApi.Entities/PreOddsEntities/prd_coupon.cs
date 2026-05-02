using PreOddsApi.Core.Model;
using System;

namespace PreOddsApi.Entities.PreOddsEntities
{
    public class prd_coupon : BaseEntity
    {
        public long prd_user_id { get; set; }
        public string total_rate { get; set; }
        public int? is_win { get; set; }
        public int? starting_at_timestamp { get; set; }
        public int? ending_at_timestamp { get; set; }
        public string coupon_id { get; set; }
    }
}
