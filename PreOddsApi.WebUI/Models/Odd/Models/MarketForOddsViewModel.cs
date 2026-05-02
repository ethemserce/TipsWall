using PreOddsApi.WebApi.Models.bookmaker.V2Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.Models.Odd.Models
{
    public class MarketForOddsViewModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public List<bookmakerForOddsV2ViewModel> Bookmarkers { get; set; }
        public int SelectedAnalysisTypeId { get; set; }
    }
}
