using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities
{
    public class OddAnalysisBusinessModel
    {
        public string AnalysisType { get; set; }
        public string WinCount { get; set; }
        public string LostCount { get; set; }
        public decimal? WinningPercent { get; set; }
        public decimal? EarningPercent { get; set; }
        public decimal? OddGroupPercent { get; set; }
    }
}