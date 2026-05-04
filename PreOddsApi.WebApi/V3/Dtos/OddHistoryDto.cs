using System;
using Newtonsoft.Json;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class OddHistoryDto
    {
        [JsonProperty("id")]
        public Guid Id { get; init; }

        [JsonProperty("fixture_id")]
        public long? FixtureId { get; init; }

        [JsonProperty("market_id")]
        public long? MarketId { get; init; }

        [JsonProperty("bookmaker_id")]
        public long? BookmakerId { get; init; }

        [JsonProperty("outcome_key")]
        public string? OutcomeKey { get; init; }

        [JsonProperty("label")]
        public string? Label { get; init; }

        [JsonProperty("value")]
        public decimal? Value { get; init; }

        [JsonProperty("bookmaker_update")]
        public DateTimeOffset? BookmakerUpdate { get; init; }

        [JsonProperty("captured_at")]
        public DateTimeOffset CapturedAt { get; init; }
    }
}
