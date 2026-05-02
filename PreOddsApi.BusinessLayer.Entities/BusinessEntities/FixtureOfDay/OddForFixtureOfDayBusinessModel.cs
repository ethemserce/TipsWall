using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities.FixtureOfDay
{
    public class OddForFixtureOfDayBusinessModel
    {
        public MarketBusinessModel Market { get; set; }
        public string OddHandicap { get; set; }
        public string OddLabel { get; set; }
        public string OddTotal { get; set; }
        public string OddValue { get; set; }
        public bool OddWinning { get; set; }
    }
}