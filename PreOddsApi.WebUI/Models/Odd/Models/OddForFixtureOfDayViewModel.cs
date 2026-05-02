using PreOddsApi.WebUI.Models.Market.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.Models.Odd.Models
{
    public class OddForFixtureOfDayViewModel
    {
        public MarketViewModel Market { get; set; }
        public string OddHandicap { get; set; }
        public string OddLabel { get; set; }
        public string OddTotal { get; set; }
        public string OddValue { get; set; }
        public bool OddWinning { get; set; }
    }
}
