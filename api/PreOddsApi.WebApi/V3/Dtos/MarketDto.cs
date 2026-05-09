
namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class MarketDto
    {
        public long Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string? DeveloperName { get; init; }

        public bool? HasWinningCalculations { get; init; }

        public bool Active { get; init; }

        public bool AvailableInStandard { get; init; }

        public bool AvailableInPremium { get; init; }
    }
}
