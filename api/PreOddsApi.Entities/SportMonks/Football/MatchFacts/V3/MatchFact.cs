using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PreOddsApi.Entities.SportMonks.Football.V3
{
    /// <summary>
    /// SportMonks Match Fact (BETA) — narrative stat per fixture from
    /// <c>/v3/football/match-facts/{fixtureId}</c>. Each fixture returns
    /// multiple rows. The <c>data</c> payload shape varies per type_id /
    /// category, so it stays as a JObject and lands in jsonb.
    /// </summary>
    public class MatchFact : SportMonksBaseEntity
    {
        [JsonProperty("sport_id")]
        public long? SportId { get; set; }

        [JsonProperty("fixture_id")]
        public long FixtureId { get; set; }

        [JsonProperty("type_id")]
        public long? TypeId { get; set; }

        [JsonProperty("participant")]
        public string? Participant { get; set; }

        [JsonProperty("basis")]
        public string? Basis { get; set; }

        [JsonProperty("data")]
        public JObject? Data { get; set; }

        [JsonProperty("natural_language")]
        public string? NaturalLanguage { get; set; }

        [JsonProperty("category")]
        public string? Category { get; set; }

        [JsonProperty("scope")]
        public string? Scope { get; set; }
    }
}
