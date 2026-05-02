using PreOddsApi.BusinessLayer.Entities.BusinessEntities;
using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Abstract
{
    public interface IStandingService
    {
        StandingBusinessModel GetStanding(long teamId);
        List<StandingBusinessModel> GetStandings(long leagueId, long seasonId, long stageId, long groupId);
    }
}
