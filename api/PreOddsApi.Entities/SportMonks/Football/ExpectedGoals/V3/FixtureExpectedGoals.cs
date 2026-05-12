using Newtonsoft.Json;

namespace PreOddsApi.Entities.SportMonks.Football.V3
{
    /// <summary>
    /// SportMonks xG row from <c>/v3/football/expected/fixtures</c>. One
    /// entry per (fixture × team × type_id) where `type_id` distinguishes
    /// the xG variant (cumulative xG, xG on target, xG against, etc.).
    /// The metric itself lives in <c>data.value</c>; the writer flattens
    /// it into a numeric column for analytics but keeps the wrapper as
    /// raw JSON so newly-added fields stay captured.
    /// </summary>
    public class FixtureExpectedGoals : SportMonksBaseEntity
    {
        [JsonProperty("fixture_id")]
        public long FixtureId { get; set; }

        [JsonProperty("type_id")]
        public long? TypeId { get; set; }

        [JsonProperty("participant_id")]
        public long? ParticipantId { get; set; }

        [JsonProperty("location")]
        public string? Location { get; set; }

        [JsonProperty("data")]
        public ExpectedGoalsData? Data { get; set; }
    }

    public class ExpectedGoalsData
    {
        [JsonProperty("value")]
        public decimal? Value { get; set; }
    }
}
