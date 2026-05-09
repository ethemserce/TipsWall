using PreOddsApi.BusinessLayer.Entities.BusinessEntities;
using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Abstract
{
    public interface ITopScorerService
    {
        TopScorerBusinessModel GetTopScorers(long leagueId, long seasonId, long stageId, string lang);
    }
}
