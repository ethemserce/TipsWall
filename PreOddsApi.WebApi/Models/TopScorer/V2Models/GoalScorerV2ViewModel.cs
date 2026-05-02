using PreOddsApi.WebApi.Models.Player.V2Models;
using PreOddsApi.WebApi.Models.Team.V2Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.TopScorer.V2Models
{
    public class GoalScorerV2ViewModel
    {
        //public long Id { get; set; }
        //public long? LeagueId { get; set; }
        //public long? PlayerId { get; set; }
        public PlayerV2ViewModel Player { get; set; }
        public int? Position { get; set; }
        //public long? SeasonId { get; set; }
        //public long? StageId { get; set; }
        //public long? TeamId { get; set; }
        public TeamV2ViewModel Team { get; set; }
        public int? Goals { get; set; }
        public int? PenaltyGoals { get; set; }
    }
}
