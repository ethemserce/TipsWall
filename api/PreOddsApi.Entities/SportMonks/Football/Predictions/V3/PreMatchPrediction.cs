using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PreOddsApi.Entities.SportMonks.Football.V3
{
    /// <summary>
    /// SportMonks pre-match prediction row from
    /// <c>/v3/football/predictions/probabilities/fixtures/{fixtureId}</c>.
    /// The `predictions` payload shape varies per type_id (1X2 scores,
    /// BTTS, total goals, etc.), so it stays as a JObject and lands in a
    /// jsonb column. Each fixture can return multiple prediction rows.
    /// </summary>
    public class PreMatchPrediction : SportMonksBaseEntity
    {
        [JsonProperty("fixture_id")]
        public long FixtureId { get; set; }

        [JsonProperty("type_id")]
        public long? TypeId { get; set; }

        [JsonProperty("predictions")]
        public JObject? Predictions { get; set; }
    }
}
