using Newtonsoft.Json;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class NewsLineDto
    {
        [JsonProperty("id")]
        public long Id { get; init; }

        [JsonProperty("text")]
        public string Text { get; init; } = string.Empty;

        [JsonProperty("type")]
        public string? Type { get; init; }

        [JsonProperty("sort_order")]
        public int? SortOrder { get; init; }
    }
}
