using AutoMapper;
using PreOddsApi.BusinessLayer.Abstract;
using PreOddsApi.BusinessLayer.Entities.BusinessEntities;
using PreOddsApi.BusinessLayer.Entities.BusinessEntities.FixtureOfDay;
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
    public class FixtureOfDayService : IFixtureOfDayService
    {
        private readonly IUnitOfWork<PreOddsApiDbContext> _unitOfWork;
        private readonly IFixtureService _fixtureService;
        private readonly IOddService _oddService;
        private readonly IMarketService _marketService;
        private readonly IMapper _mapper;

        public FixtureOfDayService(IUnitOfWork<PreOddsApiDbContext> unitOfWork, IMapper mapper, IFixtureService fixtureService, IOddService oddService, IMarketService marketService)
        {
            _unitOfWork = unitOfWork;
            _fixtureService = fixtureService;
            _oddService = oddService;
            _marketService = marketService;
            _mapper = mapper;
        }

        public List<FixtureOfDayBusinessModel> GetFixtureOfDay(string date, string timezone)
        {
            var fixtureOfDate = DateTime.UtcNow.Date;
            List<FixtureOfDayBusinessModel> model = new List<FixtureOfDayBusinessModel>();
            if (DateTime.TryParse(date, out fixtureOfDate))
            {
                List<FixtureOfDayBusinessModel> fixtureOfDayList = _mapper.Map<List<FixtureOfDayBusinessModel>>(_unitOfWork.Repository<prd_fixture_of_day>().GetList(p => p.timeStartingAtDate.Value.Date == fixtureOfDate.Date && p.flag == 1));
                foreach (var fixtureOfDay in fixtureOfDayList)
                {
                    FixtureBusinessModel fixture = _fixtureService.GetFixtureForFixtureOfDay(fixtureOfDay.FixtureId, timezone);
                    if (fixture != null)
                    {
                        fixtureOfDay.Fixture = _mapper.Map<FixtureForFixtureOfDayBusinessModel>(fixture);
                        List<OddBusinessModel> odds = _oddService.GetOdds(fixtureOfDay.FixtureId, 2, 1);
                        if (odds.Count > 0)
                        {
                            foreach (var odd in odds)
                            {
                                OddForFixtureOfDayBusinessModel fixtureOfDayOdd = _mapper.Map<OddForFixtureOfDayBusinessModel>(odd);
                                fixtureOfDayOdd.Market = _marketService.GetMarket(1);
                                fixtureOfDay.Fixture.Odds.Add(fixtureOfDayOdd);
                            }
                        }

                        model.Add(fixtureOfDay);
                    }
                }
            }
            return model;
        }

    }
}
