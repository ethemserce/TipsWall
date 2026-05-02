using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.Models.Fixture.Models
{
    public class FixtureOfRoundBaseViewModel
    {
        public string TimeStartingAtDate { get; set; }
        public List<FixtureForRoundViewModel> Fixture { get; set; }
    }
}
