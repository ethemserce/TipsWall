using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities.Fixture
{
    public class FixtureForOddAnalysisBaseBusinessModel
    {
        public FixtureForOddAnalysisBaseBusinessModel()
        {
            this.Fixture = new List<FixtureForOddAnalysisBusinessModel>();
        }
        public int Page { get; set; }
        public bool IsLastPage { get; set; }
        public List<FixtureForOddAnalysisBusinessModel> Fixture { get; set; }
        public bool Success { get; set; }
    }
}
