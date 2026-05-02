using PreOddsApi.WebApi.Models.Country.V2Models;
using PreOddsApi.WebApi.Models.Group.V2Models;
using PreOddsApi.WebApi.Models.League.V2Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Fixture.V2Models
{
    public class FixtureForLeagueBaseV2ViewModel
    {
        public string TimeStartingAtDate { get; set; }
        public int MatchCount { get; set; }
        public int LiveMatchCount { get; set; }
        public LeagueV2ViewModel League { get; set; }= new LeagueV2ViewModel();
        public CountryV2ViewModel Country { get; set; }=new CountryV2ViewModel();
        public GroupV2ViewModel Group { get; set; } = new GroupV2ViewModel();
        public List<FixtureForLeagueV2ViewModel> Fixture { get; set; } = new List<FixtureForLeagueV2ViewModel>();
    }
}
