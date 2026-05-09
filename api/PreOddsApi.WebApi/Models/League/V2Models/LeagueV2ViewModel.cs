using PreOddsApi.WebApi.Models.Country.V2Models;
using PreOddsApi.WebApi.Models.Season.V2Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.League.V2Models
{
    public class LeagueV2ViewModel
    {
        public long Id { get; set; }
        //public long CountryId { get; set; }
        //public CountryV2ViewModel Country { get; set; }
        public string Logo { get; set; }
        public string Name { get; set; }
        //public LogoViewModel LogoSet { get; set; }
        //public List<SeasonV2ViewModel> Seasons { get; set; }
    }
}
