
namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class TeamDto
    {
        public long Id { get; init; }

        public long? CountryId { get; init; }

        public long? VenueId { get; init; }

        public string Name { get; init; } = string.Empty;

        public string? ShortCode { get; init; }

        public string? ImagePath { get; init; }

        public int? Founded { get; init; }

        public string? Type { get; init; }

        public string? Gender { get; init; }
    }
}
