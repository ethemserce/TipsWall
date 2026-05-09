
namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class FixtureStatisticDto
    {
        public long TypeId { get; init; }

        public string? TypeCode { get; init; }

        public string? TypeName { get; init; }

        public decimal? HomeValue { get; init; }

        public decimal? AwayValue { get; init; }
    }
}
