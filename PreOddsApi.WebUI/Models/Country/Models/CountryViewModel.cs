using PreOddsApi.WebUI.Models.League.Models;
using PreOddsApi.WebUI.Models.Logo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.Models.Country.Models
{
    public class CountryViewModel
    {
        public long Id { get; set; }
        public string Logo { get; set; }
        public LogoViewModel LogoSet { get; set; }
        public string Name { get; set; }
        public LeagueListViewModel Leagues { get; set; }
    }
}
