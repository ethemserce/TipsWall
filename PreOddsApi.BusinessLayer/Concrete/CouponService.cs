using AutoMapper;
using PreOddsApi.BusinessLayer.Abstract;
using PreOddsApi.BusinessLayer.Entities.BusinessEntities;
using PreOddsApi.BusinessLayer.Entities.BusinessEntities.Coupon;
using PreOddsApi.BusinessLayer.Entities.BusinessEntities.User;
using PreOddsApi.Core.Data.EntityFramework.Abstract;
using PreOddsApi.DataLayer;
using PreOddsApi.Entities.PreOddsEntities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
namespace PreOddsApi.BusinessLayer.Concrete
{
    public class CouponService : ICouponService
    {
        private readonly IUnitOfWork<PreOddsApiDbContext> _unitOfWork;
        private readonly IOddService _oddService;
        private readonly IMarketService _marketService;
        private readonly IFixtureService _fixtureService;
        private readonly ITeamService _teamService;
        private readonly IMapper _mapper;

        public CouponService(IUnitOfWork<PreOddsApiDbContext> unitOfWork, IFixtureService fixtureService, ITeamService teamService, IOddService oddService, IMarketService marketService, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _oddService = oddService;
            _fixtureService = fixtureService;
            _marketService = marketService;
            _teamService = teamService;
        }

        public PrdCouponBusinessModel GetCoupon(string couponId)
        {
            return _mapper.Map<PrdCouponBusinessModel>(_unitOfWork.Repository<prd_coupon>().Get(p => p.coupon_id == couponId));
        }

        public List<PrdCouponBusinessModel> GetCoupons(string date, string timeZone)
        {
            double zone;
            if (!double.TryParse(timeZone, out zone))
            {
                return new List<PrdCouponBusinessModel>();
            }

            DateTime currentDate = DateTime.Now;
            DateTime atDate = DateTime.Parse(date);
            if (currentDate.Hour < 8)
            {
                atDate = atDate.AddDays(-1);
            }

            DateTime utcDatetime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            int utcTimestamp = (int)(atDate.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

            var coupons = _unitOfWork.Repository<prd_coupon>().GetList();
            List<PrdCouponBusinessModel> couponList = new List<PrdCouponBusinessModel>();
            foreach (var coupon in coupons)
            {
                var user = _unitOfWork.Repository<prd_user>().Get(p => p.id == coupon.prd_user_id);
                PrdCouponBusinessModel couponModel = new PrdCouponBusinessModel()
                {
                    CreateDateTime = coupon.create_date_time,
                    Id = coupon.id,
                    IsWin = coupon.is_win == null ? 2 : coupon.is_win,
                    TotalRate = coupon.total_rate,
                    PrdUserId = coupon.prd_user_id,
                    User = _mapper.Map<PrdUserBusinessModel>(user)
                };

                var couponItems = _unitOfWork.Repository<prd_coupon_item>().GetList(p => p.prd_coupon_id == coupon.id);
                couponModel.CouponItem = _mapper.Map<List<PrdCouponItemBusinessModel>>(couponItems);
                foreach (var couponItem in couponModel.CouponItem)
                {
                    var fixture = _fixtureService.GetFixtureForCoupon(couponItem.FixtureId, timeZone);

                    couponItem.MarketName = _marketService.GetMarket(couponItem.MarketId).Name;
                    couponItem.TimeStartingAtDate = fixture.TimeStartingAtDate;
                    couponItem.TimeStartingAtTime = fixture.TimeStartingAtTime;
                    couponItem.LocalTeamName = _teamService.GetTeam(fixture.LocalTeamId).Name;
                    couponItem.VisitorTeamName = _teamService.GetTeam(fixture.VisitorTeamId).Name;

                    var odds = _oddService.GetOdds(couponItem.FixtureId, couponItem.BookmarkerId, couponItem.MarketId);
                    var odd = odds.Where(p => p.OddHandicap == couponItem.OddHandicap && p.OddLabel == couponItem.OddLabel && p.OddTotal == couponItem.OddTotal).FirstOrDefault();

                    if (odd == null)
                    {
                        couponItem.OddWinning = 3;
                    }
                    else
                    {
                        if (fixture.WinningOddsCalculated)
                        {
                            couponItem.OddWinning = odd.OddWinning == false ? 0 : 1;
                        }
                        else
                        {
                            couponItem.OddWinning = 2;
                        }
                    }
                }
                couponList.Add(couponModel);
            }

            return couponList;
        }

        public void CreateCoupon(PrdCouponBusinessModel coupon)
        {
            _unitOfWork.BeginTransaction();

            try
            {
                string couponId = Guid.NewGuid().ToString();
                prd_coupon couponDB = new prd_coupon()
                {
                    create_date_time = DateTime.UtcNow,
                    is_win = null,
                    prd_user_id = coupon.PrdUserId,
                    coupon_id = couponId
                };

                PrdCouponBusinessModel currentCoupon = GetCoupon(couponId);
                if (currentCoupon != null)
                {
                    foreach (var couponItem in coupon.CouponItem)
                    {
                        prd_coupon_item couponItemDB = new prd_coupon_item()
                        {
                            //bookmaker_id = couponItem.BookmarkerId,
                            fixture_id = couponItem.FixtureId,
                            market_id = couponItem.MarketId,
                            odd_handicap = couponItem.OddHandicap,
                            odd_label = couponItem.OddLabel,
                            odd_total = couponItem.OddTotal,
                            odd_value = couponItem.OddValue,
                            odd_winning = null,
                            prd_coupon_id = currentCoupon.Id
                        };

                    }

                    _unitOfWork.Repository<prd_coupon>().Insert(couponDB);
                    _unitOfWork.Commit();
                }
                else
                {
                    _unitOfWork.Rollback();
                }
            }
            catch (Exception)
            {
                _unitOfWork.Rollback();
            }
        }
    }
}
