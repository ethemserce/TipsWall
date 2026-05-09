using PreOddsApi.Core.Model;
using System;


namespace PreOddsApi.Entities.PreOddsEntities
{
    public class seasonstats : BaseEntity
    {
        public long leagueId { get; set; }
        public league league { get; set; }
        public long seasonId { get; set; }
        public season season { get; set; }
        public int? number_of_clubs { get; set; }
        public int? number_of_matches { get; set; }
        public int? number_of_matches_played { get; set; }
        public int? number_of_goals { get; set; }
        public int? matches_both_teams_scored { get; set; }
        public int? number_of_yellowcards { get; set; }
        public int? number_of_yellowredcards { get; set; }
        public int? number_of_redcards { get; set; }
        public string avg_goals_per_match { get; set; }
        public string avg_yellowcards_per_match { get; set; }
        public string avg_yellowredcards_per_match { get; set; }
        public string avg_redcards_per_match { get; set; }
        public int? team_with_most_goals_id { get; set; }
        public int? team_with_most_conceded_goals_id { get; set; }
        public int? team_with_most_goals_per_match_id { get; set; }
        public int? season_topscorer_id { get; set; }
        public int? season_assist_topscorer_id { get; set; }
        public int? team_most_cleansheets_id { get; set; }
        public string goals_scored_minutes_0 { get; set; }
        public string goals_scored_minutes_15 { get; set; }
        public string goals_scored_minutes_30 { get; set; }
        public string goals_scored_minutes_45 { get; set; }
        public string goals_scored_minutes_60 { get; set; }
        public string goals_scored_minutes_75 { get; set; }
        public int? goalkeeper_most_cleansheets_id { get; set; }
        public int? goal_scored_every_minutes { get; set; }
    }
}