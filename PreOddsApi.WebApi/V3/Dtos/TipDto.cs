using System;
using Newtonsoft.Json;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class TipDto
    {
        [JsonProperty("id")]
        public Guid Id { get; init; }

        [JsonProperty("fixture_id")]
        public long FixtureId { get; init; }

        [JsonProperty("feed_type")]
        public string FeedType { get; init; } = string.Empty;

        [JsonProperty("bookmaker_id")]
        public long BookmakerId { get; init; }

        [JsonProperty("market_id")]
        public long MarketId { get; init; }

        [JsonProperty("outcome_key")]
        public string OutcomeKey { get; init; } = string.Empty;

        [JsonProperty("label")]
        public string Label { get; init; } = string.Empty;

        [JsonProperty("odd_value")]
        public decimal? OddValue { get; init; }

        [JsonProperty("total")]
        public string? Total { get; init; }

        [JsonProperty("handicap")]
        public string? Handicap { get; init; }

        [JsonProperty("result_status")]
        public string ResultStatus { get; init; } = string.Empty;

        [JsonProperty("note")]
        public string? Note { get; init; }

        [JsonProperty("published_at")]
        public DateTimeOffset? PublishedAt { get; init; }

        [JsonProperty("settled_at")]
        public DateTimeOffset? SettledAt { get; init; }

        [JsonProperty("created_at")]
        public DateTimeOffset CreatedAt { get; init; }
    }
}
