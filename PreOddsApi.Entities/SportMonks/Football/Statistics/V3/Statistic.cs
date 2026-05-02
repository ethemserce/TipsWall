using Newtonsoft.Json;

namespace PreOddsApi.Entities.SportMonks.Football.Statistics.V3
{
    public class Statistic : SportMonksBaseEntity
    {
        [JsonProperty("fixture_id")]
        public long FixtureId { get; set; }
        [JsonProperty("participant_id")]
        public long? TeamId { get; set; }
        [JsonProperty("type_id")]
        public long? TypeId { get; set; }
        [JsonProperty("data")]
        public StatisticData Data { get; set; }
        [JsonProperty("location")]
        public string Location { get; set; }
    }
}
