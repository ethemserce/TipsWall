using PreOddsApi.BusinessLayer.Entities.BusinessEntities;
using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Abstract
{
    public interface IBenchService
    {
        List<LineupBusinessModel> GetBenchs(long fixtureId, long teamId);
    }
}
