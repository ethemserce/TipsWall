using Newtonsoft.Json;

namespace PreOddsApi.Entities.SportMonks.Football.V3
{
    public class LineupDetail
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("fixture_id")]
        public long FixtureId { get; set; }

        [JsonProperty("player_id")]
        public long? PlayerId { get; set; }

        [JsonProperty("team_id")]
        public long TeamId { get; set; }

        [JsonProperty("lineup_id")]
        public long LineupId { get; set; }

        [JsonProperty("type_id")]
        public long TypeId { get; set; }

        [JsonProperty("data")]
        public object Data { get; set; }
    }
}
