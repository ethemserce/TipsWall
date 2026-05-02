using PreOddsApi.WebUI.Models.Player.Models;
using PreOddsApi.WebUI.Models.Team.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.Models.Scorer.Models
{
    public class GoalScorerViewModel
    {
        public long Id { get; set; }
        public long? LeagueId { get; set; }
        public long? PlayerId { get; set; }
        public PlayerViewModel Player { get; set; }
        public int? Position { get; set; }
        public long? SeasonId { get; set; }
        public long? StageId { get; set; }
        public long? TeamId { get; set; }
        public TeamViewModel Team { get; set; }
        public int? Goals { get; set; }
        public int? PenaltyGoals { get; set; }
    }
}
