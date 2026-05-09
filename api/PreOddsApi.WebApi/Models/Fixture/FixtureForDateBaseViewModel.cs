using PreOddsApi.WebApi.Models.Fixture.V2Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Fixture
{
    public class FixtureForDateBaseViewModel
    {
        //public string TimeStartingAtDate { get; set; }
        public List<FixtureForLeagueV2ViewModel> Fixture { get; set; }
    }
}
