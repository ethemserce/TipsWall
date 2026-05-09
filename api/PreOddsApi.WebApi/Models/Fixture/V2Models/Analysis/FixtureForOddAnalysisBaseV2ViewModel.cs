using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Fixture.V2Models.Analysis
{
    public class FixtureForOddAnalysisBaseV2ViewModel
    {
        public int Page { get; set; }
        public bool IsLastPage { get; set; }
        public List<FixtureForOddAnalysisV2ViewModel> Fixture { get; set; }
        public bool Success { get; set; }
    }
}
