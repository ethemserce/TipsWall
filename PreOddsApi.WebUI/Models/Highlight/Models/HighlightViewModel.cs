using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.Models.Highlight.Models
{
    public class HighlightViewModel
    {
        public long Id { get; set; }
        public long FixtureId { get; set; }
        public string Location { get; set; }
    }
}
