using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Fixture
{
    public class OddAnalysisViewModel
    {
        public string AnalysisType { get; set; }
        public string WinCount { get; set; }
        public string LostCount { get; set; }
        public decimal WinningPercent { get; set; }
        public decimal EarningPercent { get; set; }
        public decimal OddGroupPercent { get; set; }
    }
}
