using PreOddsApi.WebApi.Models.Fixture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Odds
{
    public class OddSeriesViewModel
    {
        //public List<FixtureForOddAnalysisViewModel> LocalTeamSeries { get; set; }
        //public List<FixtureForOddAnalysisViewModel> VisitorTeamSeries { get; set; }
        public OddWinLostSeriesViewModel WinLostSeries { get; set; }

    }
}
