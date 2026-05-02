using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Country.V2Models
{
    public class CountryListV2ViewModel
    {
        public CountryListV2ViewModel()
        {
            this.Countries = new List<CountryV2ViewModel>();
        }
        public List<CountryV2ViewModel> Countries { get; set; }
    }
}
