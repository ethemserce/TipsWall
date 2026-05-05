using Newtonsoft.Json;

namespace PreOddsApi.WebApi.V3.Contracts
{
    public sealed class CreateFavoriteRequest
    {
        [JsonProperty("favorite_type")]
        public string FavoriteType { get; set; } = string.Empty;

        [JsonProperty("team_id")]
        public long? TeamId { get; set; }

        [JsonProperty("league_id")]
        public long? LeagueId { get; set; }

        [JsonProperty("fixture_id")]
        public long? FixtureId { get; set; }

        [JsonProperty("notes")]
        public string? Notes { get; set; }

        [JsonProperty("sort_order")]
        public int? SortOrder { get; set; }
    }
}
