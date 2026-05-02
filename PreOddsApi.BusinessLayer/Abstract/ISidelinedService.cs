using PreOddsApi.BusinessLayer.Entities.BusinessEntities;
using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Abstract
{
    public interface ISidelinedService
    {
        List<SidelinedBusinessModel> GetSidelineds(long fixtureId, long teamId);
    }
}
