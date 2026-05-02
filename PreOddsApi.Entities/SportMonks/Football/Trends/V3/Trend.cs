using Newtonsoft.Json;
using PreOddsApi.Entities.SportMonks.Core.V3;
using PreOddsApi.Entities.SportMonks.Football.V3;

namespace PreOddsApi.Entities.SportMonks.Football
{
    public class Trend : SportMonksBaseEntity
    {
        [JsonProperty("fixture_id")]
        public long FixtureId { get; set; }

        [JsonProperty("participant_id")]
        public long? TeamId { get; set; }

        [JsonProperty("type_id")]
        public long? TypeId { get; set; }

        [JsonProperty("period_id")]
        public long? Period_id { get; set; }

        [JsonProperty("value")]
        public int Value { get; set; }

        [JsonProperty("minute")]
        public int Minute { get; set; }

        [JsonProperty("period")]
        public Period Period { get; set; }

        [JsonProperty("type")]
        public Types Type { get; set; }

        [JsonProperty("participant")]
        public Team Participant { get; set; }

        [JsonProperty("fixture")]
        public Fixture Fixture { get; set; }

    }
}
