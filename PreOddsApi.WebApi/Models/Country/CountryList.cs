using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Country
{
    public class CountryList
    {
        public CountryList()
        {
            this.Countries = new List<CountryViewModel>();
        }
        public List<CountryViewModel> Countries { get; set; }
    }
}
