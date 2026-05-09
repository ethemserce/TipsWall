using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities
{
    public class OddAnalysisBaseBusinessModel
    {
        public OddAnalysisBaseBusinessModel()
        {
            this.OddAnalysis = new List<OddAnalysisBusinessModel>();
        }
        public long Id { get; set; }
        //public long BookmarkerId { get; set; }
        //public long MarketId { get; set; }
        //public string OddLabel { get; set; }
        //public string OddTotal { get; set; }
        //public string OddValue { get; set; }
        //public string OddHandicap { get; set; }
        public List<OddAnalysisBusinessModel> OddAnalysis { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
