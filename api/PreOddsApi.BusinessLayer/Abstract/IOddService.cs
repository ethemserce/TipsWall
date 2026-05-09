using PreOddsApi.BusinessLayer.Entities.BusinessEntities;
using PreOddsApi.BusinessLayer.Entities.BusinessEntities.Fixture;
using PreOddsApi.BusinessLayer.Entities.BusinessEntities.Odd;
using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Abstract
{
    public interface IOddService
    {
        OddBusinessModel GetOdd(long id);
        List<OddBusinessModel> GetOdds(long fixtureId);
        List<OddBusinessModel> GetOdds(long fixtureId, long bookmarkerId);
        List<OddBusinessModel> GetOdds(long fixtureId, long bookmarkerId, long marketId);
        List<OddBusinessModel> GetOddsWithAnalysis(long fixtureId);
        List<OddBusinessModel> GetOddsWithAnalysis(long fixtureId, long bookmarkerId);
        List<OddBusinessModel> GetOddsWithAnalysis(long fixtureId, long bookmarkerId, long marketId);
        List<HotRateBusinessModel> GetHotRateOdds(string date, long bookmarkerId, long marketId, int winningPercent, int earningPercente, int count, int odd_value);
        List<HotRateBusinessModel> GetWinningPercenteOdds(string date, long bookmarkerId, long marketId, int winningPercent, int count, int odd_value);
        List<HotRateBusinessModel> GetEarningPercenteOdds(string date, long bookmarkerId, long marketId, int count, int odd_value);
        OddSeriesBusinessModel GetOddSeries(long oddId, string timeZone);
    }
}
