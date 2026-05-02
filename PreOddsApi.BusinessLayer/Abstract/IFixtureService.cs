using PreOddsApi.BusinessLayer.Entities.BusinessEntities;
using PreOddsApi.BusinessLayer.Entities.BusinessEntities.Fixture;
using PreOddsApi.BusinessLayer.Entities.BusinessEntities.Fixture.Live;
using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Abstract
{
    public interface IFixtureService
    {
        FixtureBusinessModel GetFixture(long fixtureId, string timeZone);
        FixtureBusinessModel GetFixtureForCoupon(long fixtureId, string timeZone);
        FixtureBusinessModel GetFixtureForTips(long fixtureId, string timeZone);
        FixtureBusinessModel GetFixtureForFixtureOfDay(long fixtureId, string timeZone);
        List<FixtureForLeagueBaseBusinessModel> GetFixtureByRoundId(long leagueId, long seasonId, long stageId, long groupId, long roundId, string timeZone);
        List<FixtureForLeagueBusinessModel> GetFixturesOfRound(long leagueId, string timeZone);
        List<FixtureForLeagueBusinessModel> GetFixturesOfRound(long leagueId, long seasonId, long stageId, long roundId, long groupId, string timeZone);
        FixtureDetailHeaderBusinessModel GetFixtureDetailHeader(long fixtureId, string timeZone);
        FixtureForLiveBusinessModel GetFixtureForLive(string date, int tarihSecim, string timeZone);
        FixtureForLiveBusinessModel GetFixtureForLiveV2(string date, int tarihSecim, int statu, string timeZone);
        FixtureForFavoriteBusinessModel GetFavoriteFixture(string fixtureIds, string timeZone);
        FixtureForOddAnalysisBaseBusinessModel GetHotRateFixtures(string date, long bookmakerId, long marketId, int winningPercent, int earningPercente, string part, string rate,int allFixture, int page, string timeZone);
        FixtureForOddAnalysisBaseBusinessModel GetWinningPercenteFixtures(string date, long bookmakerId, long marketId, int winningPercent, string part, string rate, int allFixture, int page, string timeZone);
        FixtureForOddAnalysisBaseBusinessModel GetEarningPercenteFixtures(string date, long bookmakerId, long marketId, string part, string rate, int allFixture, int page, string timeZone);
    }
}
