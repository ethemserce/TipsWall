using PreOddsApi.BusinessLayer.Entities.BusinessEntities;
using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Abstract
{
    public interface IStageService
    {
        StageBusinessModel GetStage(long stageId);
        List<StageBusinessModel> GetStages(long seasonId);
    }
}
