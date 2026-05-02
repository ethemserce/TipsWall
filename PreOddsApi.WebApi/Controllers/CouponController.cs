using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PreOddsApi.BusinessLayer.Abstract;
using AutoMapper;
using PreOddsApi.WebApi.Models.Continent;
using PreOddsApi.WebApi.Models.League;
using Microsoft.Extensions.Localization;
using System.Globalization;
using PreOddsApi.WebApi.Models.Fixture;
using PreOddsApi.WebApi.Models.Coupon;

namespace PreOddsApi.WebApi.Controllers
{
    [Produces("application/json")]
    [Route("api")]
    public class CouponController : Controller
    {
        private readonly IMapper _mapper;
        private readonly ICouponService _couponService;
        private readonly IStringLocalizer<CouponController> _couponLocalizer;
        private readonly IStringLocalizer<FixtureController> _fixtureLocalizer;
        private readonly IStringLocalizer<MarketController> _marketLocalizer;

        public CouponController(ICouponService couponService, IMapper mapper, IStringLocalizer<MarketController> marketLocalizer, IStringLocalizer<CouponController> couponLocalizer, IStringLocalizer<FixtureController> fixtureLocalizer)
        {
            _couponService = couponService;
            _couponLocalizer = couponLocalizer;
            _fixtureLocalizer = fixtureLocalizer;
            _marketLocalizer = marketLocalizer;
            _mapper = mapper;
        }

        [HttpGet]
        [Route("coupon/date={date}&lang={lang}&timeZone={timeZone}&apiKey={apiKey}")]
        public IActionResult GetCoupons(string date, string lang, string timeZone, string apiKey)
        {
            if (apiKey != "1")
            {
                return Json(null);
            }

            var model = _couponService.GetCoupons(date, timeZone);
            List<CouponViewModel> couponList = new List<CouponViewModel>();
            foreach (var coupon in model.OrderByDescending(p=>p.CreateDateTime))
            {
                CouponViewModel couponModel = new CouponViewModel()
                {
                    CreateDateTime = coupon.CreateDateTime,
                    Id = coupon.Id,
                    PrdUserId = coupon.PrdUserId,
                    TotalRate = coupon.TotalRate,
                    UserLogo = coupon.User.Avatar,
                    UserName = coupon.User.NickName,
                    IsWin = coupon.IsWin
                };

                foreach (var couponItem in coupon.CouponItem)
                {
                    couponItem.MarketName = SetMarketLang("market" + couponItem.MarketId);
                    couponItem.OddLabel = SetOddLabelLang(couponItem.OddLabel);
                    CouponItemViewModel couponItemModel = new CouponItemViewModel()
                    {
                        Id = couponItem.Id,
                        FixtureId = couponItem.FixtureId,
                        LocalTeamName = couponItem.LocalTeamName,
                        MarketName = couponItem.MarketName,
                        OddLabel = (couponItem.OddTotal + " " + couponItem.OddLabel + " " + couponItem.OddHandicap).Trim(),
                        OddValue = couponItem.OddValue,
                        PrdCouponId = couponItem.PrdCouponId,
                        TimeStartingAtDate = couponItem.TimeStartingAtDate,
                        TimeStartingAtTime = couponItem.TimeStartingAtTime,
                        VisitorTeamName = couponItem.VisitorTeamName,
                        OddWinning = couponItem.OddWinning
                    };

                    couponModel.CouponItem.Add(couponItemModel);
                }

               
                couponList.Add(couponModel);
            }
            return Json(couponList);
        }

        private string SetMarketLang(string market)
        {
            return _couponLocalizer[market.Trim()];
        }

        private string SetOddLabelLang(string label)
        {
            return _couponLocalizer["_" + label.Trim().Replace(" ", "_").Replace("/", "_")];
        }
    }
}