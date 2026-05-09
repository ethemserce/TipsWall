using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities
{
    public class TvstationBusinessModel
    {
        public long Id { get; set; }
        public DateTime CreateDateTime { get; set; }
        public long FixtureId { get; set; }
        public string Name { get; set; }
        public DateTime UpdateDateTime { get; set; }
    }
}
