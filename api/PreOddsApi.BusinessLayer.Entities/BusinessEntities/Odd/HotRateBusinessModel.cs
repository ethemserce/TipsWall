using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities.Odd
{
    public class HotRateBusinessModel
    {
        public long Id { get; set; }
        public string OddLabel { get; set; }
        public string OddTotal { get; set; }
        public string OddValue { get; set; }
        public string OddHandicap { get; set; }
        public string WinCount { get; set; }
        public string LostCount { get; set; }
        public decimal? WinningPercent { get; set; }
        public decimal? EarningPercent { get; set; }
        public decimal? OddGroupPercent { get; set; }
    }
}
