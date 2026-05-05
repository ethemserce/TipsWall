using System;
using Newtonsoft.Json;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class FavoriteDto
    {
        [JsonProperty("id")]
        public Guid Id { get; init; }

        [JsonProperty("favorite_type")]
        public string FavoriteType { get; init; } = string.Empty;

        [JsonProperty("team_id")]
        public long? TeamId { get; init; }

        [JsonProperty("league_id")]
        public long? LeagueId { get; init; }

        [JsonProperty("fixture_id")]
        public long? FixtureId { get; init; }

        [JsonProperty("notes")]
        public string? Notes { get; init; }

        [JsonProperty("sort_order")]
        public int? SortOrder { get; init; }

        [JsonProperty("created_at")]
        public DateTimeOffset CreatedAt { get; init; }
    }
}
