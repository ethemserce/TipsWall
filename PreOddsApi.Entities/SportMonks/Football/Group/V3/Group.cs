using Newtonsoft.Json;
using System;

namespace PreOddsApi.Entities.SportMonks.Football.V3
{
    public class Group : SportMonksBaseEntity
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

        [JsonProperty("starting_at")]
        public DateTime? StartingAt { get; set; }

        [JsonProperty("ending_at")]
        public DateTime? EndingAt { get; set; }
    }
}
