using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities.Tips
{
    public class TipsBaseBusinessModel
    {
        public int Page { get; set; }
        public bool IsLastPage { get; set; }
        public bool Success { get; set; }
        public List<TipsBusinessModel> Tips { get; set; }
    }
}
