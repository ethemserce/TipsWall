using Newtonsoft.Json;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class BookmakerDto
    {
        [JsonProperty("id")]
        public long Id { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; } = string.Empty;

        [JsonProperty("logo_path")]
        public string? LogoPath { get; init; }

        [JsonProperty("active")]
        public bool Active { get; init; }

        [JsonProperty("available_in_standard")]
        public bool AvailableInStandard { get; init; }

        [JsonProperty("available_in_premium")]
        public bool AvailableInPremium { get; init; }
    }
}
