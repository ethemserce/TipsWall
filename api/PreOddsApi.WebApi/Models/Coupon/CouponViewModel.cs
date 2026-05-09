using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Coupon
{
    public class CouponViewModel
    {
        public CouponViewModel()
        {
            this.CouponItem = new List<CouponItemViewModel>();
        }
        public long Id { get; set; }
        public long PrdUserId { get; set; }
        public string UserName { get; set; }
        public string UserLogo { get; set; }
        public DateTime CreateDateTime { get; set; }
        public string TotalRate { get; set; }
        public int? IsWin { get; set; }
        public List<CouponItemViewModel> CouponItem { get; set; }
    }
}
