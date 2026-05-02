using PreOddsApi.WebApi.Models.Country.V2Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Continent.V2Models
{
    public class ContinentV2ViewModel
    {
        //public long Id { get; set; }
        public string Name { get; set; }
        public List<CountryV2ViewModel> Countries { get; set; }
    }
}
