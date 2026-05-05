using Newtonsoft.Json;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class TeamDto
    {
        [JsonProperty("id")]
        public long Id { get; init; }

        [JsonProperty("country_id")]
        public long? CountryId { get; init; }

        [JsonProperty("venue_id")]
        public long? VenueId { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; } = string.Empty;

        [JsonProperty("short_code")]
        public string? ShortCode { get; init; }

        [JsonProperty("image_path")]
        public string? ImagePath { get; init; }

        [JsonProperty("founded")]
        public int? Founded { get; init; }

        [JsonProperty("type")]
        public string? Type { get; init; }

        [JsonProperty("gender")]
        public string? Gender { get; init; }
    }
}
