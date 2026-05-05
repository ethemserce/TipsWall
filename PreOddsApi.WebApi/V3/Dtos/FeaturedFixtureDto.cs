using System;
using Newtonsoft.Json;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class FeaturedFixtureDto
    {
        [JsonProperty("id")]
        public Guid Id { get; init; }

        [JsonProperty("fixture_id")]
        public long FixtureId { get; init; }

        [JsonProperty("feature_date")]
        public DateTime FeatureDate { get; init; }

        [JsonProperty("source")]
        public string Source { get; init; } = string.Empty;

        [JsonProperty("title")]
        public string? Title { get; init; }

        [JsonProperty("description")]
        public string? Description { get; init; }

        [JsonProperty("priority")]
        public int Priority { get; init; }

        [JsonProperty("active")]
        public bool Active { get; init; }

        [JsonProperty("created_at")]
        public DateTimeOffset CreatedAt { get; init; }
    }
}
