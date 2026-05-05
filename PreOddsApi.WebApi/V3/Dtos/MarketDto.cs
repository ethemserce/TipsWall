using Newtonsoft.Json;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class MarketDto
    {
        [JsonProperty("id")]
        public long Id { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; } = string.Empty;

        [JsonProperty("developer_name")]
        public string? DeveloperName { get; init; }

        [JsonProperty("has_winning_calculations")]
        public bool? HasWinningCalculations { get; init; }

        [JsonProperty("active")]
        public bool Active { get; init; }

        [JsonProperty("available_in_standard")]
        public bool AvailableInStandard { get; init; }

        [JsonProperty("available_in_premium")]
        public bool AvailableInPremium { get; init; }
    }
}
