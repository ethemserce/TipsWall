using System;
using Newtonsoft.Json;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class PrematchOddDto
    {
        [JsonProperty("id")]
        public long Id { get; init; }

        [JsonProperty("fixture_id")]
        public long FixtureId { get; init; }

        [JsonProperty("market_id")]
        public long MarketId { get; init; }

        [JsonProperty("bookmaker_id")]
        public long BookmakerId { get; init; }

        [JsonProperty("outcome_key")]
        public string OutcomeKey { get; init; } = string.Empty;

        [JsonProperty("label")]
        public string Label { get; init; } = string.Empty;

        [JsonProperty("value")]
        public decimal? Value { get; init; }

        [JsonProperty("probability")]
        public decimal? Probability { get; init; }

        [JsonProperty("american")]
        public int? American { get; init; }

        [JsonProperty("fractional")]
        public string? Fractional { get; init; }

        [JsonProperty("winning")]
        public bool? Winning { get; init; }

        [JsonProperty("stopped")]
        public bool? Stopped { get; init; }

        [JsonProperty("total")]
        public string? Total { get; init; }

        [JsonProperty("handicap")]
        public string? Handicap { get; init; }

        [JsonProperty("captured_at")]
        public DateTimeOffset CapturedAt { get; init; }

        [JsonProperty("last_synced_at")]
        public DateTimeOffset? LastSyncedAt { get; init; }
    }
}
