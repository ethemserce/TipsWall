using PreOddsApi.BusinessLayer.Entities.BusinessEntities;
using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Abstract
{
    public interface ILineupService
    {
        List<LineupBusinessModel> GetLineups(long fixtureId, long teamId);
    }
}
