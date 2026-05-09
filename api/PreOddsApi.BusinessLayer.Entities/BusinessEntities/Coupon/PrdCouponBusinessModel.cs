using PreOddsApi.BusinessLayer.Entities.BusinessEntities.User;
using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities.Coupon
{
    public class PrdCouponBusinessModel
    {
        public PrdCouponBusinessModel()
        {
            this.CouponItem = new List<PrdCouponItemBusinessModel>();
        }
        public long Id { get; set; }
        public long PrdUserId { get; set; }
        public PrdUserBusinessModel User { get; set; }
        public DateTime CreateDateTime { get; set; }
        public string TotalRate { get; set; }
        public int? IsWin { get; set; }
        public int? StartingAtTimestamp { get; set; }
        public int? EndingAtTimestamp { get; set; }
        public string CouponId { get; set; }
        public List<PrdCouponItemBusinessModel> CouponItem { get; set; }
    }
}