using PreOddsApi.BusinessLayer.Entities.BusinessEntities;
using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Abstract
{
    public interface IMarketService
    {
        MarketBusinessModel GetMarket(long id);
        List<MarketBusinessModel> GetMarkets();
        List<MarketBusinessModel> GetMarkets(long fixtureId);
        List<MarketBusinessModel> GetMarkets(long fixtureId, long bookmarkerId);
        List<MarketBusinessModel> GetMarkets(long fixtureId, long bookmarkerId, long marketId);
    }
}
