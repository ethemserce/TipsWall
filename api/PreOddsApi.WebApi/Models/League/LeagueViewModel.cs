using PreOddsApi.WebApi.Models.Country;
using PreOddsApi.WebApi.Models.Fixture;
using System;
using System.Collections.Generic;

namespace PreOddsApi.WebApi.Models.League
{
    public class LeagueViewModel
    {
        public LeagueViewModel()
        {
            this.Seasons = new List<SeasonViewModel>();
        }
        public long Id { get; set; }
        public long CountryId { get; set; }
        public CountryViewModel Country { get; set; }
        //public DateTime CreateDateTime { get; set; }
        //public bool Cup { get; set; }
        //public int LegacyId { get; set; }
        public string Logo { get; set; }
        public string Name { get; set; }
        public DateTime UpdateDateTime { get; set; }
        public LogoViewModel LogoSet { get; set; }
        //public int Status { get; set; }
        public List<SeasonViewModel> Seasons { get; set; }
        //public List<FixtureForLeagueBaseViewModel> Fixtures { get; set; }
    }
}
