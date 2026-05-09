using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Fixture
{
    public class TvstationViewModel
    {
        public long Id { get; set; }
        public DateTime CreateDateTime { get; set; }
        public long FixtureId { get; set; }
        public string Name { get; set; }
        public DateTime UpdateDateTime { get; set; }
    }
}
