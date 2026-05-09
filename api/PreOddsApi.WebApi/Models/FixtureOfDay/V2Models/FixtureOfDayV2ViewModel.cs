using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.FixtureOfDay.V2Models
{
    public class FixtureOfDayV2ViewModel
    {
        public long Id { get; set; }
        public long FixtureId { get; set; }
        public DateTime TimeStartingAtDate { get; set; }
        public DateTime CreateDateTime { get; set; }
        public int Flag { get; set; }
        public FixtureForFixtureOfDayV2ViewModel Fixture { get; set; }
    }
}
