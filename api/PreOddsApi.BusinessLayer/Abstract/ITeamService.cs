using PreOddsApi.BusinessLayer.Entities.BusinessEntities;
using PreOddsApi.Entities.PreOddsEntities;
using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Abstract
{
    public interface ITeamService
    {
        TeamBusinessModel GetTeam(long teamId);
        TeamBusinessModel ConvertTeamToBusinessModel(team team);
    }
}
