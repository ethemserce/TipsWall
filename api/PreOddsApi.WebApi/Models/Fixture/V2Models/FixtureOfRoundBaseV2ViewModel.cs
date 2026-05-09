using PreOddsApi.WebApi.Models.Country.V2Models;
using PreOddsApi.WebApi.Models.League.V2Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Fixture.V2Models
{
    public class FixtureOfRoundBaseV2ViewModel
    {
        public FixtureOfRoundBaseV2ViewModel()
        {
            this.Fixture = new List<FixtureForRoundV2ViewModel>();
        }
        public string TimeStartingAtDate { get; set; }
        //public LeagueV2ViewModel League { get; set; }
        //public CountryV2ViewModel Country { get; set; }
        public List<FixtureForRoundV2ViewModel> Fixture { get; set; }
    }
}
