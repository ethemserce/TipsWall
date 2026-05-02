using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.Models.Fixture.Models
{
    public class FixtureForDateBaseViewModel
    {
        public string TimeStartingAtDate { get; set; }
        public List<FixtureForLeagueViewModel> Fixture { get; set; }
    }
}
