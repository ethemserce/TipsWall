using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.Models.Scorer.Models
{
    public class TopScorerViewModel
    {
        public List<AssistsScorerViewModel> AssistsScorer { get; set; }
        public List<CardScorerViewModel> CardScorer { get; set; }
        public List<GoalScorerViewModel> GoalScorer { get; set; }
    }
}
