using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace PreOddsApi.Entities.SportMonks.Football.V3
{
    public class Schedule
    {
        [JsonProperty("id")]
        public long Id { get; set; }

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
        public DateTimeOffset? StartingAt { get; set; }

        [JsonProperty("ending_at")]
        public DateTimeOffset? EndingAt { get; set; }

        [JsonProperty("games_in_current_week")]
        public bool GamesInCurrentWeek { get; set; }

        [JsonProperty("tie_breaker_rule_id")]
        public int TieBreakerRuleId { get; set; }

        [JsonProperty("rounds")] // if add to url "include=participants"
        public List<ScheduleRound> Rounds { get; set; }
    }
}
