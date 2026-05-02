using Newtonsoft.Json;
using System;

namespace PreOddsApi.Entities.SportMonks.Football.V3
{
    public class Squad
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("transfer_id")]
        public long TransferId { get; set; }

        [JsonProperty("player_id")]
        public long PlayerId { get; set; }

        [JsonProperty("team_id")]
        public long TeamId { get; set; }

        [JsonProperty("position_id")]
        public int PositionId { get; set; }

        [JsonProperty("detailed_position_id")]
        public int DetailedPositionId { get; set; }

        [JsonProperty("start")]
        public DateOnly Start { get; set; }

        [JsonProperty("end")]
        public DateOnly End { get; set; }

        [JsonProperty("captain")]
        public bool Captain { get; set; }

        [JsonProperty("jersey_number")]
        public int JerseyNumber { get; set; }
    }
}
