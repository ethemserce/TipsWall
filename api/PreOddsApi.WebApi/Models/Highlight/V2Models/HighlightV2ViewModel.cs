using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Highlight.V2Models
{
    public class HighlightV2ViewModel
    {
        public long Id { get; set; }
        public long FixtureId { get; set; }
        public string Location { get; set; }
    }
}
