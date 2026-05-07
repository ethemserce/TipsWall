using Newtonsoft.Json;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class FixtureStatisticDto
    {
        [JsonProperty("type_id")]
        public long TypeId { get; init; }

        [JsonProperty("type_code")]
        public string? TypeCode { get; init; }

        [JsonProperty("type_name")]
        public string? TypeName { get; init; }

        [JsonProperty("home_value")]
        public decimal? HomeValue { get; init; }

        [JsonProperty("away_value")]
        public decimal? AwayValue { get; init; }
    }
}
