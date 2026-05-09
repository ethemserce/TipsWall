using Newtonsoft.Json;

namespace PreOddsApi.Entities.SportMonks.Football.V3
{
    public class BallCoordinate
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("fixture_id")]
        public long FixtureId { get; set; }

        [JsonProperty("period_id")]
        public long PeriodId { get; set; }

        [JsonProperty("timer")]
        public string Timer { get; set; }

        [JsonProperty("x")]
        public string X { get; set; }

        [JsonProperty("y")]
        public string Y { get; set; }
    }
}
