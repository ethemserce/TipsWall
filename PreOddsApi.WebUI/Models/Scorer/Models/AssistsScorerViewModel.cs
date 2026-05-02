using PreOddsApi.WebUI.Models.Player.Models;
using PreOddsApi.WebUI.Models.Team.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.Models.Scorer.Models
{
    public class AssistsScorerViewModel
    {
        public long Id { get; set; }
        public int? Assists { get; set; }
        public long? LeagueId { get; set; }
        public long? PlayerId { get; set; }
        public PlayerViewModel Player { get; set; }
        public int? Position { get; set; }
        public long? SeasonId { get; set; }
        public long? StageId { get; set; }
        public long? TeamId { get; set; }
        public TeamViewModel Team { get; set; }
    }
}
