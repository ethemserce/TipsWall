using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Fixture
{
    public class OddAnalysisBaseViewModel
    {
        public OddAnalysisBaseViewModel()
        {
            this.OddAnalysis = new List<OddAnalysisViewModel>();
        }
        public long Id { get; set; }
        //public long bookmaker_id { get; set; }
        //public long MarketId { get; set; }
        //public string OddLabel { get; set; }
        //public string OddTotal { get; set; }
        //public string OddValue { get; set; }
        //public string OddHandicap { get; set; }
        public List<OddAnalysisViewModel> OddAnalysis { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
