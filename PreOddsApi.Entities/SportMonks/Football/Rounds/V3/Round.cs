using Newtonsoft.Json;
using PreOddsApi.Entities.SportMonks.Core.V3;
using System;
using System.Collections.Generic;

namespace PreOddsApi.Entities.SportMonks.Football.V3
{
    public class Round : SportMonksBaseEntity
    {
        [JsonProperty("sport_id")]
        public long SportId { get; set; }

        [JsonProperty("league_id")]
        public long LeagueId { get; set; }

        [JsonProperty("season_id")]
        public long SeasonId { get; set; }

        [JsonProperty("stage_id")]
        public long StageId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

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

        [JsonProperty("sport")] // if add to Query Params "include=sport"
        public Sport Sport { get; set; }

        [JsonProperty("league")] // if add to Query Params "include=league"
        public League League { get; set; }

        [JsonProperty("season")] // if add to Query Params "include=season"
        public Season Season { get; set; }

        [JsonProperty("stage")] // if add to Query Params "include=stage"
        public Stage Stage { get; set; }

        [JsonProperty("fixture")] // if add to Query Params "include=fixture"
        public List<Fixture> Fixtures { get; set; }

        //[JsonProperty("statistics")] // if add to Query Params "include=statistics"
        //public List<Statistic> Stages { get; set; }
    }
}
