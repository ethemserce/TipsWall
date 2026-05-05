using System;
using Newtonsoft.Json;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class CouponItemDto
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

        [JsonProperty("sort_order")]
        public int SortOrder { get; init; }
    }
}
