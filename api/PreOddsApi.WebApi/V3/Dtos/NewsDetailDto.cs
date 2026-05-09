using System.Collections.Generic;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class NewsDetailDto
    {
        public NewsSummaryDto News { get; init; } = new();

        public IReadOnlyList<NewsLineDto> Lines { get; init; } = new List<NewsLineDto>();
    }
}
