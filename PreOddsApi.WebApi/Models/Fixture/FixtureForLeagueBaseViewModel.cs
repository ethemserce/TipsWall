using PreOddsApi.WebApi.Models.Country;
using PreOddsApi.WebApi.Models.League;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Fixture
{
    public class FixtureForLeagueBaseViewModel
    {
        public FixtureForLeagueBaseViewModel()
        {
            this.Fixture = new List<FixtureForLeagueViewModel>();
        }
        public string TimeStartingAtDate { get; set; }
        public LeagueViewModel League { get; set; }
        public CountryViewModel Country { get; set; }
        public List<FixtureForLeagueViewModel> Fixture { get; set; }
    }
}