using Newtonsoft.Json;

namespace PreOddsApi.WebApi.V3.Contracts
{
    public sealed class CreateTipRequest
    {
        [JsonProperty("fixture_id")]
        public long FixtureId { get; set; }

        [JsonProperty("odds_current_id")]
        public long? OddsCurrentId { get; set; }

        [JsonProperty("feed_type")]
        public string FeedType { get; set; } = "standard";

        [JsonProperty("bookmaker_id")]
        public long BookmakerId { get; set; }

        [JsonProperty("market_id")]
        public long MarketId { get; set; }

        [JsonProperty("outcome_key")]
        public string OutcomeKey { get; set; } = string.Empty;

        [JsonProperty("label")]
        public string Label { get; set; } = string.Empty;

        [JsonProperty("odd_value")]
        public decimal? OddValue { get; set; }

        [JsonProperty("total")]
        public string? Total { get; set; }

        [JsonProperty("handicap")]
        public string? Handicap { get; set; }

        [JsonProperty("note")]
        public string? Note { get; set; }

        [JsonProperty("visibility")]
        public string Visibility { get; set; } = "public";
    }
}
