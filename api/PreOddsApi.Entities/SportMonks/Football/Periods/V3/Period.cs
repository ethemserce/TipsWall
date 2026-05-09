using Newtonsoft.Json;

namespace PreOddsApi.Entities.SportMonks.Football.V3
{
    public class Period : SportMonksBaseEntity
    {
        [JsonProperty("fixture_id")]
        public long FixtureId { get; set; }

        [JsonProperty("type_id")]
        public long TypeId { get; set; }

        [JsonProperty("started")]
        public long Started { get; set; }

        [JsonProperty("ended")]
        public long? Ended { get; set; }

        [JsonProperty("counts_from")]
        public int CountsFrom { get; set; }

        [JsonProperty("actual_period_start")]
        public int ActualPeriodStart { get; set; }

        [JsonProperty("ticking")]
        public bool Ticking { get; set; }

        [JsonProperty("sort_order")]
        public int SortOrder { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("time_added")]
        public int? TimeAdded { get; set; }

        [JsonProperty("minutes")]
        public int Minutes { get; set; }

        [JsonProperty("seconds")]
        public int Seconds { get; set; }
    }
}
