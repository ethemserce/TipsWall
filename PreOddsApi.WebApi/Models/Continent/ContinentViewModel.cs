using PreOddsApi.WebApi.Models.Country;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Continent
{
    public class ContinentViewModel
    {
        public long Id { get; set; }
        public DateTime CreateDateTime { get; set; }
        public string Name { get; set; }
        public DateTime UpdateDateTime { get; set; }
        public List<CountryViewModel> Countries { get; set; }
    }
}
