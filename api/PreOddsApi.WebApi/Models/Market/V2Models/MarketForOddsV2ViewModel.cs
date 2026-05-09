using PreOddsApi.WebApi.Models.bookmaker.V2Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Market.V2Models
{
    public class MarketForOddsV2ViewModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public long SelectedAnalysisTypeId { get; set; }
        public List<bookmakerForOddsV2ViewModel> Bookmakers { get; set; }
    }
}
