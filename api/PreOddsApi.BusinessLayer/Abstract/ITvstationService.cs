using PreOddsApi.BusinessLayer.Entities.BusinessEntities;
using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Abstract
{
    public interface ITvstationService
    {
        List<TvstationBusinessModel> GetTvstations(long fixtureId);
    }
}
