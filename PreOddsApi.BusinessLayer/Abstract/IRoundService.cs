using PreOddsApi.BusinessLayer.Entities.BusinessEntities;
using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Abstract
{
    public interface IRoundService
    {
        RoundBusinessModel GetRound(long roundId);
        List<RoundBusinessModel> GetRounds(long stageId);
    }
}
