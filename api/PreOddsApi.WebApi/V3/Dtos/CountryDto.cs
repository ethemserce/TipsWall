
namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class CountryDto
    {
        public long Id { get; init; }

        public long ContinentId { get; init; }

        public string Name { get; init; } = string.Empty;

        public string? OfficialName { get; init; }

        public string? Iso2 { get; init; }

        public string? Iso3 { get; init; }

        public string? ImagePath { get; init; }
    }
}
