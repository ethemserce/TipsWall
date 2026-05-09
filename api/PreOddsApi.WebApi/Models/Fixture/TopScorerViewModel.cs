using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Fixture
{
    public class TopScorerViewModel
    {
        public TopScorerViewModel()
        {
            this.AssistsScorer = new List<AssistsscorerViewModel>();
            this.CardScorer = new List<CardscorerViewModel>();
            this.GoalScorer = new List<GoalscorerViewModel>();
        }
        public List<AssistsscorerViewModel> AssistsScorer { get; set; }
        public List<CardscorerViewModel> CardScorer { get; set; }
        public List<GoalscorerViewModel> GoalScorer { get; set; }
    }
}
