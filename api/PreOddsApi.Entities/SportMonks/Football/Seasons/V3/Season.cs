using Newtonsoft.Json;
using PreOddsApi.Entities.SportMonks.Core.V3;
using System;
using System.Collections.Generic;

namespace PreOddsApi.Entities.SportMonks.Football.V3
{
    public class Season : SportMonksBaseEntity
    {
        [JsonProperty("sport_id")]
        public long SportId { get; set; }

        [JsonProperty("league_id")]
        public long LeagueId { get; set; }

        // Nullable: SportMonks omits / nulls this for some leagues (the
        // upgraded plan surfaces lower-tier comps where the rule isn't set).
        [JsonProperty("tie_breaker_rule_id")]
        public long? TieBreakerRuleId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("finished")]
        public bool Finished { get; set; }

        [JsonProperty("pending")]
        public bool Pending { get; set; }

        [JsonProperty("is_current")]
        public bool IsCurrent { get; set; }

        [JsonProperty("starting_at")]
        public DateTime? StartingAt { get; set; }

        [JsonProperty("ending_at")]
        public DateTime? EndingAt { get; set; }

        [JsonProperty("standings_recalculated_at")]
        public DateTime? StandingsRecalculatedAt { get; set; }

        [JsonProperty("games_in_current_week")]
        public bool GamesInCurrentWeek { get; set; }

        [JsonProperty("sport")] // if add to Query Params "include=sport"
        public Sport Sport { get; set; }

        [JsonProperty("league")] // if add to Query Params "include=league"
        public League League { get; set; }

        [JsonProperty("teams")] // if add to Query Params "include=teams"
        public List<Team> Teams { get; set; }

        [JsonProperty("stages")] // if add to Query Params "include=stages"
        public List<Stage> Stages { get; set; }

        [JsonProperty("currentStage")] // if add to Query Params "include=currentStage"
        public Stage CurrentStage { get; set; }

        [JsonProperty("fixtures")] // if add to Query Params "include=fixtures"
        public List<Fixture> Fixtures { get; set; }

        [JsonProperty("groups")] // if add to Query Params "include=groups"
        public List<Group> Groups { get; set; }

        //[JsonProperty("statistics")] // if add to Query Params "include=statistics"
        //public List<Statistic> Stages { get; set; }

        [JsonProperty("topscorers")] // if add to Query Params "include=topscorers"
        public List<TopScorer> TopScorers { get; set; }
        
    }
}
