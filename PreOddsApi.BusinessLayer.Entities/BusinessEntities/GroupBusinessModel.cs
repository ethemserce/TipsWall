using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities
{
    public class GroupBusinessModel
    {
        public long Id { get; set; }
        public DateTime CreateDateTime { get; set; }
        public string Name { get; set; }
        public long RoundId { get; set; }
        public long StageId { get; set; }
        public DateTime UpdateDateTime { get; set; }
    }
}
