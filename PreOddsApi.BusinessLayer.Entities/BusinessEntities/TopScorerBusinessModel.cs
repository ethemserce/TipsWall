using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities
{
    public class TopScorerBusinessModel
    {
        public TopScorerBusinessModel()
        {
            this.AssistsScorer = new List<AssistsscorerBusinessModel>();
            this.CardScorer = new List<CardscorerBusinessModel>();
            this.GoalScorer = new List<GoalscorerBusinessModel>();
        }
        public List<AssistsscorerBusinessModel> AssistsScorer { get; set; }
        public List<CardscorerBusinessModel> CardScorer { get; set; }
        public List<GoalscorerBusinessModel> GoalScorer { get; set; }
    }
}
