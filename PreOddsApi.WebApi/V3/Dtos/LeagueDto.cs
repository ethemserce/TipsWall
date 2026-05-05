using Newtonsoft.Json;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class LeagueDto
    {
        [JsonProperty("id")]
        public long Id { get; init; }

        [JsonProperty("sport_id")]
        public long SportId { get; init; }

        [JsonProperty("country_id")]
        public long? CountryId { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; } = string.Empty;

        [JsonProperty("active")]
        public bool Active { get; init; }

        [JsonProperty("short_code")]
        public string? ShortCode { get; init; }

        [JsonProperty("image_path")]
        public string? ImagePath { get; init; }

        [JsonProperty("type")]
        public string? Type { get; init; }

        [JsonProperty("sub_type")]
        public string? SubType { get; init; }

        [JsonProperty("category")]
        public int? Category { get; init; }
    }
}
