using PreOddsApi.WebUI.Models.Bookmarker.Models;
using PreOddsApi.WebUI.Models.Market.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.Models.Odd.Models
{
    public class OddViewModel
    {
        public OddViewModel()
        {
            this.OddAnalysis = new List<OddAnalysisViewModel>();
        }
        public long Id { get; set; }
        public BookmarkerViewModel Bookmarker { get; set; }
        public long FixtureId { get; set; }
        public long MarketId { get; set; }
        public MarketViewModel Market { get; set; }
        public string OddHandicap { get; set; }
        public string OddLabel { get; set; }
        public string OddTotal { get; set; }
        public string OddValue { get; set; }
        public bool OddWinning { get; set; }
        public List<OddAnalysisViewModel> OddAnalysis { get; set; }
    }
}
