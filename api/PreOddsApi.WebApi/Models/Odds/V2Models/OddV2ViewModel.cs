using PreOddsApi.WebApi.Models.bookmaker.V2Models;
using PreOddsApi.WebApi.Models.Market.V2Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Odds.V2Models
{
    public class OddV2ViewModel
    {
        public long Id { get; set; }
        public bookmakerV2ViewModel bookmaker { get; set; }
        public long FixtureId { get; set; }
        public long MarketId { get; set; }
        public MarketV2ViewModel Market { get; set; }
        public string OddHandicap { get; set; }
        public string OddLabel { get; set; }
        public string OddTotal { get; set; }
        public string OddValue { get; set; }
        public bool OddWinning { get; set; }
        public List<OddAnalysisV2ViewModel> OddAnalysis { get; set; }
    }
}
