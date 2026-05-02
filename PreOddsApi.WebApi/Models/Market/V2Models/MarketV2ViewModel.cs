using PreOddsApi.WebApi.Models.bookmaker.V2Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Market.V2Models
{
    public class MarketV2ViewModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        //public List<bookmakerV2ViewModel> bookmakers { get; set; }
    }
}
