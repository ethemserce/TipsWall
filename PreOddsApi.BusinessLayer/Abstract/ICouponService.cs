using PreOddsApi.BusinessLayer.Entities.BusinessEntities.Coupon;
using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Abstract
{
    public interface ICouponService
    {
        List<PrdCouponBusinessModel> GetCoupons(string date, string timeZone);
    }
}
