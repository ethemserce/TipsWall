using PreOddsApi.BusinessLayer.Entities.BusinessEntities;
using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Abstract
{
    public interface IBookmakerService
    {
        BookmakerBusinessModel GetBookmaker(long bookmarkerId);
        List<BookmakerBusinessModel> GetBookmaker();
    }
}
