using PreOddsApi.WebUI.Models.Country.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.Models.Continent.Models
{
    public class ContinentViewModel
    {
        public string Name { get; set; }
        public List<CountryViewModel> Countries { get; set; }
    }
}
