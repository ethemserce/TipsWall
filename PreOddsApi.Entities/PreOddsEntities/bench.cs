using PreOddsApi.Core.Model;

namespace PreOddsApi.Entities.PreOddsEntities
{
   public class bench : BaseEntity
    {
        public int? additional_position { get; set; }
        public long? fixtureId { get; set; }
        public fixture fixture { get; set; }
        public int? formation_position { get; set; }
        public int? number { get; set; }
        public long? playerId { get; set; }
        public player player { get; set; }
        public string player_name { get; set; }
        public string position { get; set; }
        public int? posx { get; set; }
        public int? posy { get; set; }
        public int? stats_cards_redcards { get; set; }
        public int? stats_cards_yellowcards { get; set; }
        public int? stats_fouls_committed { get; set; }
        public int? stats_fouls_drawn { get; set; }
        public int? stats_goals_conceded { get; set; }
        public int? stats_goals_scored { get; set; }
        public int? stats_other_assists { get; set; }
        public int? stats_other_blocks { get; set; }
        public int? stats_other_clearances { get; set; }
        public int? stats_other_hit_woodwork { get; set; }
        public int? stats_other_interceptions { get; set; }
        public int? stats_other_minutes_played { get; set; }
        public int? stats_other_offsides { get; set; }
        public int? stats_other_pen_committed { get; set; }
        public int? stats_other_pen_missed { get; set; }
        public int? stats_other_pen_saved { get; set; }
        public int? stats_other_pen_scored { get; set; }
        public int? stats_other_pen_won { get; set; }
        public int? stats_other_saves { get; set; }
        public int? stats_other_tackles { get; set; }
        public int? stats_passing_crosses_accuracy { get; set; }
        public int? stats_passing_passes { get; set; }
        public int? stats_passing_passes_accuracy { get; set; }
        public int? stats_passing_total_crosses { get; set; }
        public int? stats_shots_shots_on_goal { get; set; }
        public int? stats_shots_shots_total { get; set; }
        public long? teamId { get; set; }
        public team team { get; set; }
    }
}
