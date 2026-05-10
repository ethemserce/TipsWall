using Newtonsoft.Json;

namespace PreOddsApi.Entities.SportMonks.Football.V3
{
    /// <summary>
    /// SportMonks pre-match value-bet from
    /// <c>/v3/football/predictions/value-bets/fixtures/{fixtureId}</c>.
    /// Each row marks one (bookmaker, outcome) pair that the model rates
    /// as positive expected value against its `fair_odd`. The writer
    /// serialises Predictions back to jsonb so newly-added fields are
    /// captured in the raw column even if they're not yet promoted.
    /// </summary>
    public class ValueBet : SportMonksBaseEntity
    {
        [JsonProperty("fixture_id")]
        public long FixtureId { get; set; }

        [JsonProperty("type_id")]
        public long? TypeId { get; set; }

        [JsonProperty("predictions")]
        public ValueBetPrediction? Predictions { get; set; }
    }

    public class ValueBetPrediction
    {
        [JsonProperty("bet")]
        public string? Bet { get; set; }

        [JsonProperty("bookmaker")]
        public string? Bookmaker { get; set; }

        [JsonProperty("fair_odd")]
        public decimal? FairOdd { get; set; }

        [JsonProperty("odd")]
        public decimal? Odd { get; set; }

        [JsonProperty("stake")]
        public decimal? Stake { get; set; }

        [JsonProperty("is_value")]
        public bool? IsValue { get; set; }
    }
}
