using System;
using Newtonsoft.Json;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class CouponSummaryDto
    {
        [JsonProperty("id")]
        public Guid Id { get; init; }

        [JsonProperty("public_code")]
        public string PublicCode { get; init; } = string.Empty;

        [JsonProperty("title")]
        public string? Title { get; init; }

        [JsonProperty("total_rate")]
        public decimal? TotalRate { get; init; }

        [JsonProperty("status")]
        public string Status { get; init; } = string.Empty;

        [JsonProperty("starts_at")]
        public DateTimeOffset? StartsAt { get; init; }

        [JsonProperty("ends_at")]
        public DateTimeOffset? EndsAt { get; init; }

        [JsonProperty("published_at")]
        public DateTimeOffset? PublishedAt { get; init; }

        [JsonProperty("settled_at")]
        public DateTimeOffset? SettledAt { get; init; }

        [JsonProperty("created_at")]
        public DateTimeOffset CreatedAt { get; init; }
    }
}
