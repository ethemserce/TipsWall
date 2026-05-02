using PreOddsApi.WebUI.Models.Logo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.Models.League.Models
{
    public class LeagueViewModel
    {
        public long Id { get; set; }
        public string Logo { get; set; }
        public string Name { get; set; }
        public LogoViewModel LogoSet { get; set; }
    }
}
