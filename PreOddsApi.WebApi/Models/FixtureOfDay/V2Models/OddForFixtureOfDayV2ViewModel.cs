using PreOddsApi.WebApi.Models.Market.V2Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.FixtureOfDay.V2Models
{
    public class OddForFixtureOfDayV2ViewModel
    {
        public MarketV2ViewModel Market { get; set; }
        public string OddHandicap { get; set; }
        public string OddLabel { get; set; }
        public string OddTotal { get; set; }
        public string OddValue { get; set; }
        public bool OddWinning { get; set; }
    }
}
