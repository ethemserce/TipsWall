using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Fixture
{
    public class HighlightViewModel
    {
        public long Id { get; set; }
        public DateTime CreateDateTime { get; set; }
        public long FixtureId { get; set; }
        public string Location { get; set; }
        public DateTime UpdateDateTime { get; set; }
    }
}
