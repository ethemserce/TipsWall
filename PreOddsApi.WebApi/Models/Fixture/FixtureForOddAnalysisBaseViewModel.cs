using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Fixture
{
    public class FixtureForOddAnalysisBaseViewModel
    {
        public int Page { get; set; }
        public bool IsLastPage { get; set; }
        public List<FixtureForOddAnalysisViewModel> Fixture { get; set; }
        public bool Success { get; set; }
    }
}
