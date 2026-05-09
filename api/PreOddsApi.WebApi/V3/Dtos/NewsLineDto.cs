
namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class NewsLineDto
    {
        public long Id { get; init; }

        public string Text { get; init; } = string.Empty;

        public string? Type { get; init; }

        public int? SortOrder { get; init; }
    }
}
