using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities
{
    public class OddBusinessModel
    {
        public OddBusinessModel()
        {
            this.OddAnalysis = new List<OddAnalysisBusinessModel>();
        }
        public long Id { get; set; }
        public long BookmakerId { get; set; }
        public BookmakerBusinessModel Bookmaker { get; set; }
        public DateTime CreateDateTime { get; set; }
        public long FixtureId { get; set; }
        public long MarketId { get; set; }
        public MarketBusinessModel Market { get; set; }
        public string OddHandicap { get; set; }
        public string OddLabel { get; set; }
        public string OddTotal { get; set; }
        public string OddValue { get; set; }
        public bool OddWinning { get; set; }
        public DateTime UpdateDateTime { get; set; }
        public List<OddAnalysisBusinessModel> OddAnalysis { get; set; }
    }
}
