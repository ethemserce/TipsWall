using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Fixture
{
    public class CornerViewModel
    {
        public long Id { get; set; }
        public string Comment { get; set; }
        public DateTime CreateDateTime { get; set; }
        public int ExtraMinute { get; set; }
        public long FixtureId { get; set; }
        public int Minute { get; set; }
        public long TeamId { get; set; }
        public DateTime UpdateDateTime { get; set; }
    }
}
