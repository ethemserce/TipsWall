using System;
using Newtonsoft.Json;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class NewsSummaryDto
    {
        [JsonProperty("id")]
        public long Id { get; init; }

        [JsonProperty("fixture_id")]
        public long? FixtureId { get; init; }

        [JsonProperty("league_id")]
        public long? LeagueId { get; init; }

        [JsonProperty("title")]
        public string Title { get; init; } = string.Empty;

        [JsonProperty("type")]
        public string? Type { get; init; }

        [JsonProperty("created_at")]
        public DateTimeOffset CreatedAt { get; init; }
    }
}
