using Newtonsoft.Json;
using PreOddsApi.Entities.SportMonks.Core.V3;
using PreOddsApi.Entities.SportMonks.Football.V3;

namespace PreOddsApi.Entities.SportMonks.Football
{
    public class Trend : SportMonksBaseEntity
    {
        [JsonProperty("fixture_id")]
        public long? FixtureId { get; set; }

        [JsonProperty("participant_id")]
        public long? ParticipantId { get; set; }

        [JsonProperty("type_id")]
        public long? TypeId { get; set; }

        [JsonProperty("period_id")]
        public long? PeriodId { get; set; }

        [JsonProperty("value")]
        public decimal? Value { get; set; }

        [JsonProperty("pressure")]
        public decimal? Pressure { get; set; }

        [JsonProperty("minute")]
        public int? Minute { get; set; }

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
