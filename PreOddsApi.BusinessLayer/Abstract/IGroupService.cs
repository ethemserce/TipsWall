using PreOddsApi.BusinessLayer.Entities.BusinessEntities;
using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Abstract
{
    public interface IGroupService
    {
        GroupBusinessModel GetGroup(long? groupId);
        GroupBusinessModel GetGroup(long roundId, long stageId);
        List<GroupBusinessModel> GetGroups(long stageId);
    }
}
