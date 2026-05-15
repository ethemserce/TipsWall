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

        // SportMonks ships `null` for these on non-ticking periods (e.g.
        // the synthetic "extra time" placeholder period that appears before
        // ET starts). Newtonsoft.Json on a non-nullable int throws
        // `Error converting value {null} to type 'System.Int32'`, which
        // aborts the *entire* livescores/latest batch — every live fixture
        // stays frozen until the worker restarts. Keep these nullable.
        [JsonProperty("minutes")]
        public int? Minutes { get; set; }

        [JsonProperty("seconds")]
        public int? Seconds { get; set; }
    }
}
