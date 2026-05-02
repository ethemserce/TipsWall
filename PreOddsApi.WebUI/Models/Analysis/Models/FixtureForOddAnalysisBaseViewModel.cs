using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.Models.Analysis.Models
{
    public class FixtureForOddAnalysisBaseViewModel
    {
        public int Page { get; set; }
        public bool IsLastPage { get; set; }
        public List<FixtureForOddAnalysisViewModel> Fixture { get; set; }
        public bool Success { get; set; }
    }
}
