using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities
{
    public class RoundBusinessModel
    {
        public long Id { get; set; }
        public DateTime CreateDateTime { get; set; }
        public string End { get; set; }
        public int Name { get; set; }
        public long StageId { get; set; }
        public string Start { get; set; }
        public DateTime UpdateDateTime { get; set; }
    }
}
