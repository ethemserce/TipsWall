using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Fixture
{
    public class OddViewModel
    {
        public long Id { get; set; }
        public long bookmaker_id { get; set; }
        public bookmakerViewModel bookmaker { get; set; }
        public DateTime CreateDateTime { get; set; }
        public long FixtureId { get; set; }
        public long MarketId { get; set; }
        public MarketViewModel Market { get; set; }
        public string OddHandicap { get; set; }
        public string OddLabel { get; set; }
        public string OddTotal { get; set; }
        public string OddValue { get; set; }
        public bool OddWinning { get; set; }
        public DateTime UpdateDateTime { get; set; }
        public List<OddAnalysisViewModel> OddAnalysis { get; set; }
    }
}
