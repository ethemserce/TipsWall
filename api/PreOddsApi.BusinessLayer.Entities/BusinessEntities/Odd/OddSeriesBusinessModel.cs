using PreOddsApi.BusinessLayer.Entities.BusinessEntities.Fixture;
using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities.Odd
{
    public class OddSeriesBusinessModel
    {
        public OddSeriesBusinessModel()
        {
            //this.LocalTeamSeries = new List<FixtureForOddAnalysisBusinessModel>();
            //this.VisitorTeamSeries = new List<FixtureForOddAnalysisBusinessModel>();
        }
        //public List<FixtureForOddAnalysisBusinessModel> LocalTeamSeries { get; set; }
        //public List<FixtureForOddAnalysisBusinessModel> VisitorTeamSeries { get; set; }
        public OddWinLostSeriesBusinessModel WinLostSeries { get; set; }
    }
}
