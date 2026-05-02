using Newtonsoft.Json;
using PreOddsApi.Entities.SportMonks.Core.V3;
using System;
using System.Collections.Generic;

namespace PreOddsApi.Entities.SportMonks.Football.V3
{
    public class Stage : SportMonksBaseEntity
    {
        [JsonProperty("sport_id")]
        public long SportId { get; set; }

        [JsonProperty("league_id")]
        public long LeagueId { get; set; }

        [JsonProperty("season_id")]
        public long SeasonId { get; set; }

        [JsonProperty("type_id")]
        public long TypeId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("sort_order")]
        public int SortOrder { get; set; }

        [JsonProperty("finished")]
        public bool Finished { get; set; }

        [JsonProperty("is_current")]
        public bool IsCurrent { get; set; }

        [JsonProperty("starting_at")]
        public DateTime? StartingAt { get; set; }

        [JsonProperty("ending_at")]
        public DateTime? EndingAt { get; set; }

        [JsonProperty("games_in_current_week")]
        public bool GamesInCurrentWeek { get; set; }

        [JsonProperty("tie_breaker_rule_id")]
        public long? TieBreakerRuleId { get; set; }

        [JsonProperty("sport")] // if add to Query Params "include=sport"
        public Sport Sport { get; set; }

        [JsonProperty("league")] // if add to Query Params "include=league"
        public League League { get; set; }

        [JsonProperty("season")] // if add to Query Params "include=season"
        public Season Season { get; set; }

        [JsonProperty("type")] // if add to Query Params "include=type"
        public Types Type { get; set; }

        [JsonProperty("rounds")] // if add to Query Params "include=rounds"
        public List<Round> Rounds { get; set; }

        [JsonProperty("currentRound")] // if add to Query Params "include=currentRound"
        public Round CurrentRound { get; set; }

        [JsonProperty("groups")] // if add to Query Params "include=groups"
        public List<Group> Groups { get; set; }

        [JsonProperty("fixture")] // if add to Query Params "include=fixture"
        public List<Fixture> Fixtures { get; set; }

        [JsonProperty("aggregates")] // if add to Query Params "include=aggregates"
        public List<Aggregate> Aggregates { get; set; }

        [JsonProperty("topscorers")] // if add to Query Params "include=topscorers"
        public List<TopScorer> TopScorers { get; set; }

        //[JsonProperty("statistics")] // if add to Query Params "include=statistics"
        //public List<Statistic> Stages { get; set; }
    }
}
