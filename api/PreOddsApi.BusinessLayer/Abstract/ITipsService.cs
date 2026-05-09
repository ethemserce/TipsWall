using PreOddsApi.BusinessLayer.Entities.BusinessEntities.Tips;
using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Abstract
{
    public interface ITipsService
    {
        TipsBusinessModel GetTips(long id, string timeZone);
        TipsBaseBusinessModel GetTips(string timeZone, int page);
        TipsBaseBusinessModel GetTips2(string timeZone, int page);
        bool InsertTips(long oddId, long prdUserId);
    }
}
