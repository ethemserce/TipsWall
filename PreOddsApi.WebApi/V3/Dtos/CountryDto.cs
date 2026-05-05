using Newtonsoft.Json;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class CountryDto
    {
        [JsonProperty("id")]
        public long Id { get; init; }

        [JsonProperty("continent_id")]
        public long ContinentId { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; } = string.Empty;

        [JsonProperty("official_name")]
        public string? OfficialName { get; init; }

        [JsonProperty("iso2")]
        public string? Iso2 { get; init; }

        [JsonProperty("iso3")]
        public string? Iso3 { get; init; }

        [JsonProperty("image_path")]
        public string? ImagePath { get; init; }
    }
}
