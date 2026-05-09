
namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class BookmakerDto
    {
        public long Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string? LogoPath { get; init; }

        public bool Active { get; init; }

        public bool AvailableInStandard { get; init; }

        public bool AvailableInPremium { get; init; }
    }
}
