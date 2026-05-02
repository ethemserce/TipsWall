using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Fixture.Live
{
    public class FixtureForLiveLeagueViewModel
    {
        public long LeagueId { get; set; }
        public string LeagueName { get; set; }
        public long CountryId { get; set; }
        public string CountryName { get; set; }
        public string Logo { get; set; }
        public bool Favorite { get; set; }
        public List<FixtureForLiveItemViewModel> Data { get; set; }
    }
}
