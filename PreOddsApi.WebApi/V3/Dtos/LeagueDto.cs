
namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class LeagueDto
    {
        public long Id { get; init; }

        public long SportId { get; init; }

        public long? CountryId { get; init; }

        public string Name { get; init; } = string.Empty;

        public bool Active { get; init; }

        public string? ShortCode { get; init; }

        public string? ImagePath { get; init; }

        public string? Type { get; init; }

        public string? SubType { get; init; }

        public int? Category { get; init; }
    }
}
