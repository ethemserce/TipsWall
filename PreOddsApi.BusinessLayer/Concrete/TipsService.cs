using PreOddsApi.BusinessLayer.Abstract;
using AutoMapper;
using PreOddsApi.BusinessLayer.Entities.BusinessEntities;
using PreOddsApi.Core.Data.EntityFramework.Abstract;
using PreOddsApi.DataLayer;
using PreOddsApi.Entities.PreOddsEntities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PreOddsApi.BusinessLayer.Entities.BusinessEntities.Tips;

namespace PreOddsApi.BusinessLayer.Concrete
{
    public class TipsService : ITipsService
    {
        private readonly IUnitOfWork<PreOddsApiDbContext> _unitOfWork;
        private readonly IAnalysisUnitOfWork<PreOddsApiDbContext> _analysisUnitOfWork;
        private readonly IOddService _oddService;
        private readonly IFixtureService _fixtureService;
        private readonly IPrdUserService _prdUserService;
        private readonly IMarketService _marketService;
        private readonly IMapper _mapper;

        public TipsService(IUnitOfWork<PreOddsApiDbContext> unitOfWork, IAnalysisUnitOfWork<PreOddsApiDbContext> analysisUnitOfWork, IOddService oddService, IFixtureService fixtureService, IPrdUserService prdUserService, IMarketService marketService, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _oddService = oddService;
            _fixtureService = fixtureService;
            _prdUserService = prdUserService;
            _marketService = marketService;
            _mapper = mapper;
            _analysisUnitOfWork = analysisUnitOfWork;
        }

        public TipsBusinessModel GetTips(long id, string timeZone)
        {
            var tips = _mapper.Map<TipsBusinessModel>(_analysisUnitOfWork.Repository<prd_tips>().Get(p => p.id == id));
            if (tips == null)
            {
                return new TipsBusinessModel();
            }

            var odd = _oddService.GetOdd(tips.OddId);
            if (odd == null)
            {
                return new TipsBusinessModel();
            }

            var fixture = _fixtureService.GetFixtureForTips(odd.FixtureId, timeZone);
            if (fixture == null)
            {
                return new TipsBusinessModel();
            }

            var user = _prdUserService.GetUser(tips.PrdUserId);
            if (user == null)
            {
                return new TipsBusinessModel();
            }

            var market = _marketService.GetMarket(odd.MarketId);
            if (market == null)
            {
                return new TipsBusinessModel();
            }

            tips.OddLabel = odd.OddLabel;
            tips.OddHandicap = odd.OddHandicap;
            tips.OddTotal = odd.OddTotal;
            tips.FixtureId = fixture.Id;
            tips.LocalTeamScore = fixture.LocalTeamScore;
            tips.HtScore = fixture.HtScore;
            tips.CountryId = fixture.League.CountryId;
            tips.CountryName = fixture.League.Country.Name;
            tips.CountryLogo = fixture.League.Country.ImagePath;
            tips.LeagueName = fixture.League.Name;
            tips.LeagueLogo = fixture.League.Logo;
            tips.LocalTeamName = fixture.LocalTeam.Name;
            tips.LocalTeamLogo = fixture.LocalTeam.ImagePath;
            tips.TimeStartingAtDateTime = fixture.TimeStartingAtDateTime;
            tips.TimeStartingAtDate = fixture.TimeStartingAtDate;
            tips.TimeStartingAtTime = fixture.TimeStartingAtTime;
            tips.TimeStartingAtTimestamp = fixture.TimeStartingAtTimestamp;
            tips.TimeStatus = fixture.TimeStatus;
            tips.VisitorTeamName = fixture.VisitorTeam.Name;
            tips.VisitorTeamLogo = fixture.VisitorTeam.ImagePath;
            tips.VisitorTeamScore = fixture.VisitorTeamScore;
            tips.PrdUserId = user.Id;
            tips.UserName = user.NickName;
            tips.UserLogo = user.Avatar;
            tips.MarketName = market.Name;
            tips.MarketId = market.Id;
            if (fixture.WinningOddsCalculated)
            {
                tips.IsWin = odd.OddWinning == true ? 1 : 0;
            }
            else
            {
                tips.IsWin = 2;
            }

            return tips;
        }

        public TipsBaseBusinessModel GetTips(string timeZone, int page)
        {
            int take = 20;
            TipsBaseBusinessModel tipsBase = new TipsBaseBusinessModel();
            List<TipsBusinessModel> tipsList = new List<TipsBusinessModel>();

            var tipList = _analysisUnitOfWork.Repository<prd_tips>()
                                     .GetList()
                                     .OrderByDescending(p => p.prd_user_id)
                                     .Skip(page * take)
                                     .Take(take)
                                     .ToList();

            var tips = _mapper.Map<List<TipsBusinessModel>>(tipList);
            if (tips.Count < take)
            {
                tipsBase.IsLastPage = true;
            }
            else
            {
                tipsBase.IsLastPage = false;
            }

            foreach (var tip in tips)
            {
                var odd = _oddService.GetOdd(tip.OddId);
                if (odd == null)
                {
                    continue;
                }

                var fixture = _fixtureService.GetFixtureForTips(odd.FixtureId, timeZone);
                if (fixture == null)
                {
                    continue;
                }

                var user = _prdUserService.GetUser(tip.PrdUserId);
                if (user == null)
                {
                    continue;
                }

                var market = _marketService.GetMarket(odd.MarketId);
                if (market == null)
                {
                    continue;
                }

                tip.OddLabel = odd.OddLabel;
                tip.OddHandicap = odd.OddHandicap;
                tip.OddTotal = odd.OddTotal;
                tip.FixtureId = fixture.Id;
                tip.LocalTeamScore = fixture.LocalTeamScore;
                tip.HtScore = fixture.HtScore;
                tip.CountryId = fixture.League.CountryId;
                tip.CountryName = fixture.League.Country.Name;
                tip.CountryLogo = fixture.League.Country.ImagePath;
                tip.LeagueId = fixture.LeagueId;
                tip.LeagueName = fixture.League.Name;
                tip.LeagueLogo = fixture.League.Logo;
                tip.LocalTeamName = fixture.LocalTeam.Name;
                tip.LocalTeamLogo = fixture.LocalTeam.ImagePath;
                tip.TimeStartingAtDateTime = fixture.TimeStartingAtDateTime;
                tip.TimeStartingAtDate = fixture.TimeStartingAtDate;
                tip.TimeStartingAtTime = fixture.TimeStartingAtTime;
                tip.TimeStartingAtTimestamp = fixture.TimeStartingAtTimestamp;
                tip.TimeStatus = fixture.TimeStatus;
                tip.VisitorTeamName = fixture.VisitorTeam.Name;
                tip.VisitorTeamLogo = fixture.VisitorTeam.ImagePath;
                tip.VisitorTeamScore = fixture.VisitorTeamScore;
                tip.PrdUserId = user.Id;
                tip.UserName = user.NickName;
                tip.UserLogo = user.Avatar;
                tip.MarketName = market.Name;
                tip.MarketId = market.Id;
                if (fixture.WinningOddsCalculated && DateTime.UtcNow.AddMinutes(double.Parse(timeZone)) > DateTime.Parse(fixture.TimeStartingAtDateTime).AddHours(2))
                {
                    tip.IsWin = odd.OddWinning == true ? 1 : 0;
                }
                else
                {
                    tip.IsWin = 2;
                }

                tipsList.Add(tip);
            }

            tipsBase.Tips = tipsList;
            tipsBase.Page = page;
            tipsBase.Success = true;
            return tipsBase;
        }

        public TipsBaseBusinessModel GetTips2(string timeZone, int page)
        {
            int take = 20;
            TipsBaseBusinessModel tipsBase = new TipsBaseBusinessModel();
            List<TipsBusinessModel> tipsList = new List<TipsBusinessModel>();

            var tips = _mapper.Map<List<TipsBusinessModel>>(_analysisUnitOfWork.Repository<prd_tips>().GetList().OrderByDescending(p => p.prd_user_id).Skip(page * take).Take(take));
            if (tips.Count < take)
            {
                tipsBase.IsLastPage = true;
            }
            else
            {
                tipsBase.IsLastPage = false;
            }

            foreach (var tip in tips)
            {
                var odd = _oddService.GetOdd(tip.OddId);
                if (odd == null)
                {
                    continue;
                }

                var fixture = _fixtureService.GetFixtureForTips(odd.FixtureId, timeZone);
                if (fixture == null)
                {
                    continue;
                }

                var user = _prdUserService.GetUser(tip.PrdUserId);
                if (user == null)
                {
                    continue;
                }

                var market = _marketService.GetMarket(odd.MarketId);
                if (market == null)
                {
                    continue;
                }

                tip.OddLabel = odd.OddLabel;
                tip.OddHandicap = odd.OddHandicap;
                tip.OddTotal = odd.OddTotal;
                tip.FixtureId = fixture.Id;
                tip.LocalTeamScore = fixture.LocalTeamScore;
                tip.HtScore = fixture.HtScore;
                tip.CountryId = fixture.League.CountryId;
                tip.CountryName = fixture.League.Country.Name;
                tip.CountryLogo = fixture.League.Country.ImagePath;
                tip.LeagueId = fixture.LeagueId;
                tip.LeagueName = fixture.League.Name;
                tip.LeagueLogo = fixture.League.Logo;
                tip.LocalTeamName = fixture.LocalTeam.Name;
                tip.LocalTeamLogo = fixture.LocalTeam.ImagePath;
                tip.TimeStartingAtDateTime = fixture.TimeStartingAtDateTime;
                tip.TimeStartingAtDate = fixture.TimeStartingAtDate;
                tip.TimeStartingAtTime = fixture.TimeStartingAtTime;
                tip.TimeStartingAtTimestamp = fixture.TimeStartingAtTimestamp;
                tip.TimeStatus = fixture.TimeStatus;
                tip.VisitorTeamName = fixture.VisitorTeam.Name;
                tip.VisitorTeamLogo = fixture.VisitorTeam.ImagePath;
                tip.VisitorTeamScore = fixture.VisitorTeamScore;
                tip.PrdUserId = user.Id;
                tip.UserName = user.NickName;
                tip.UserLogo = user.Avatar;
                tip.MarketName = market.Name;
                tip.MarketId = market.Id;

                if (fixture.WinningOddsCalculated && DateTime.UtcNow.AddMinutes(double.Parse(timeZone)) > DateTime.Parse(fixture.TimeStartingAtDateTime).AddHours(2))
                {
                    tip.IsWin = odd.OddWinning == true ? 1 : 0;
                }
                else
                {
                    tip.IsWin = 2;
                }

                tipsList.Add(tip);
            }

            tipsBase.Tips = tipsList;
            tipsBase.Page = page;
            tipsBase.Success = true;
            return tipsBase;
        }

        public bool InsertTips(long oddId, long prdUserId)
        {
            var odd = _oddService.GetOdd(oddId);
            if (odd == null)
            {
                return false;
            }

            prd_tips tips = new prd_tips()
            {
                //create_date_time = DateTime.UtcNow,
                is_win = null,
                odd_id = odd.Id,
                odd_value = odd.OddValue,
                prd_user_id = prdUserId
            };

            _analysisUnitOfWork.Repository<prd_tips>().Insert(tips);
            _analysisUnitOfWork.SaveChanges();

            return true;
        }

    }
}
