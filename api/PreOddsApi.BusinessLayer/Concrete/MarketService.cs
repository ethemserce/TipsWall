using AutoMapper;
using PreOddsApi.BusinessLayer.Abstract;
using PreOddsApi.BusinessLayer.Entities.BusinessEntities;
using PreOddsApi.Core.Data.EntityFramework.Abstract;
using PreOddsApi.DataLayer;
using PreOddsApi.Entities.PreOddsEntities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PreOddsApi.BusinessLayer.Concrete
{
    public class MarketService : IMarketService
    {
        private readonly IUnitOfWork<PreOddsApiDbContext> _unitOfWork;
        private readonly IOddService _oddService;
        private readonly IBookmakerService _bookmarkerService;
        private readonly IMapper _mapper;

        public MarketService(IUnitOfWork<PreOddsApiDbContext> unitOfWork, IOddService oddService, IBookmakerService bookmarkerService, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _oddService = oddService;
            _bookmarkerService = bookmarkerService;
            _mapper = mapper;
        }

        public MarketBusinessModel GetMarket(long id)
        {
            return _mapper.Map<MarketBusinessModel>(_unitOfWork.Repository<market>().Get(p => p.id == id));
        }

        public List<MarketBusinessModel> GetMarkets()
        {
            return _mapper.Map<List<MarketBusinessModel>>(_unitOfWork.Repository<market>().GetList(p => p.flag == 1 && p.has_winning_calculations == true).OrderBy(p => p.id));
        }

        public List<MarketBusinessModel> GetMarkets(long fixtureId)
        {
            var odds = _oddService.GetOddsWithAnalysis(fixtureId);
            var oddMarketGroup = odds.GroupBy(p => p.MarketId);
            var oddBookmarkerGroup = odds.GroupBy(p => p.BookmakerId);

            List<MarketBusinessModel> marketList = new List<MarketBusinessModel>();
            foreach (var marketGroup in oddMarketGroup)
            {
                var market = _mapper.Map<MarketBusinessModel>(_unitOfWork.Repository<market>().Get(p => p.id == marketGroup.Key && p.flag == 1 && p.has_winning_calculations == true));
                if (market == null || market.Flag == 0)
                {
                    continue;
                }

                List<BookmakerBusinessModel> bookmarkerList = new List<BookmakerBusinessModel>();
                foreach (var bookmarkerGroup in oddBookmarkerGroup)
                {
                    var bookmarker = _bookmarkerService.GetBookmaker(bookmarkerGroup.Key);
                    if (bookmarker == null)
                    {
                        continue;
                    }
                    bookmarker.Odd = odds.Where(p => p.MarketId == marketGroup.Key && p.BookmakerId == bookmarkerGroup.Key).OrderBy(p => p.Id).ToList();

                    if (bookmarker.Odd.Count > 0)
                    {
                        bookmarkerList.Add(bookmarker);
                    }
                }

                if (bookmarkerList.Count > 0)
                {
                    market.Bookmakers = bookmarkerList;
                    marketList.Add(market);
                }
            }

            return marketList.OrderBy(p => p.Id).ToList();
        }

        public List<MarketBusinessModel> GetMarkets(long fixtureId, long bookmakerId)
        {
            var odds = _oddService.GetOddsWithAnalysis(fixtureId, bookmakerId);
            var oddMarketGroup = odds.GroupBy(p => p.MarketId);

            
            List<MarketBusinessModel> marketList = new List<MarketBusinessModel>();
            foreach (var marketGroup in oddMarketGroup)
            {
                foreach (var item in GetMarkets(fixtureId, bookmakerId, marketGroup.Key))
                {
                    marketList.Add(item);
                }

                //var market = _mapper.Map<MarketBusinessModel>(_unitOfWork.Repository<market>().Get(p => p.flag == 1 && p.id == marketGroup.Key));
                //if (market == null || market.Flag == 0)
                //{
                //    continue;
                //}
                //var bookmarker = _bookmarkerService.GetBookmarker(bookmarkerId);
                //if (bookmarker == null)
                //{
                //    continue;
                //}
                //bookmarker.Odd = odds.Where(p => p.MarketId == marketGroup.Key && p.BookmarkerId == bookmarkerId).OrderBy(p => p.Id).ToList();

                //if (bookmarker.Odd.Count > 0)
                //{
                //    market.Bookmarkers.Add(bookmarker);
                //    marketList.Add(market);
                //}
            }

            return marketList.OrderBy(p => p.Id).ToList();
        }

        public List<MarketBusinessModel> GetMarkets(long fixtureId, long bookmarkerId, long marketId)
        {
            List<MarketBusinessModel> marketList = new List<MarketBusinessModel>();

            var market = _mapper.Map<MarketBusinessModel>(_unitOfWork.Repository<market>().Get(p => p.flag == 1 && p.id == marketId));
            if (market == null)
            {
                return new List<MarketBusinessModel>();
            }

            var bookmarker = _bookmarkerService.GetBookmaker(bookmarkerId);
            if (bookmarker == null)
            {
                return new List<MarketBusinessModel>();
            }

            bookmarker.Odd = _oddService.GetOddsWithAnalysis(fixtureId, bookmarkerId, marketId);

            if (bookmarker.Odd.Count > 0)
            {
                market.Bookmakers.Add(bookmarker);
                marketList.Add(market);
            }

            //foreach (var marketGroup in oddMarketGroup)
            //{
            //    var market = _mapper.Map<MarketBusinessModel>(_unitOfWork.Repository<market>().Get(p => p.id == marketGroup.Key));
            //    if (market == null || market.Flag == 0)
            //    {
            //        continue;
            //    }
            //    var bookmarker = _bookmarkerService.GetBookmarker(bookmarkerId);
            //    if (bookmarker == null)
            //    {
            //        continue;
            //    }

            //    bookmarker.Odd = odds.Where(p => p.MarketId == marketGroup.Key && p.BookmarkerId == bookmarkerId).OrderBy(p => p.Id).ToList();

            //    if (bookmarker.Odd.Count > 0)
            //    {
            //        market.Bookmarkers.Add(bookmarker);
            //        marketList.Add(market);
            //    }
            //}


            return marketList.OrderBy(p => p.Id).ToList();
        }
    }
}
