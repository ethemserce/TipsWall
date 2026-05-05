using System.Collections.Generic;
using Newtonsoft.Json;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class NewsDetailDto
    {
        [JsonProperty("news")]
        public NewsSummaryDto News { get; init; } = new();

        [JsonProperty("lines")]
        public IReadOnlyList<NewsLineDto> Lines { get; init; } = new List<NewsLineDto>();
    }
}
