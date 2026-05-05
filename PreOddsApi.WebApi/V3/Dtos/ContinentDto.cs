using Newtonsoft.Json;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class ContinentDto
    {
        [JsonProperty("id")]
        public long Id { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; } = string.Empty;

        [JsonProperty("code")]
        public string? Code { get; init; }
    }
}
