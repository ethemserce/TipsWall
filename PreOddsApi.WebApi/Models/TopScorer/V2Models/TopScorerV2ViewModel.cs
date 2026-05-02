using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.TopScorer.V2Models
{
    public class TopScorerV2ViewModel
    {
        public TopScorerV2ViewModel()
        {
            this.AssistsScorer = new List<AssistsScorerV2ViewModel>();
            this.CardScorer = new List<CardScorerV2ViewModel>();
            this.GoalScorer = new List<GoalScorerV2ViewModel>();
        }
        public List<AssistsScorerV2ViewModel> AssistsScorer { get; set; }
        public List<CardScorerV2ViewModel> CardScorer { get; set; }
        public List<GoalScorerV2ViewModel> GoalScorer { get; set; }
    }
}