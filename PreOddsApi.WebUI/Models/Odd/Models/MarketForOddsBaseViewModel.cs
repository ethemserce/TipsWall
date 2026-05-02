using PreOddsApi.WebApi.Models.Market.V2Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.Models.Odd.Models
{
    public class MarketForOddsBaseViewModel
    {
        public List<MarketForOddsV2ViewModel> Markets { get; set; }
    }
}
