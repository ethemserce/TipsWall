using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities
{
    public class StageBusinessModel
    {
        public long Id { get; set; }
        public DateTime CreateDateTime { get; set; }
        public string Name { get; set; }
        public long SeasonId { get; set; }
        public string Type { get; set; }
        public DateTime UpdateDateTime { get; set; }
    }
}
