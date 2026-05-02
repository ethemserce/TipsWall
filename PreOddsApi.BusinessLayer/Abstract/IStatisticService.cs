using PreOddsApi.BusinessLayer.Entities.BusinessEntities;
using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Abstract
{
    public interface IStatisticService
    {
        List<StatisticBusinessModel> GetStatistics(long fixtureId, long teamId);
        List<StatisticBusinessModel> GetStatistics(long fixtureId);
        SeasonStatsBusinessModel GetSeasonStats(long leagueId, long seasonId);
    }
}
