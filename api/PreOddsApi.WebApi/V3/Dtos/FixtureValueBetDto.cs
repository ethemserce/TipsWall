namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class FixtureValueBetDto
    {
        public long Id { get; init; }
        public long? TypeId { get; init; }
        public string? TypeName { get; init; }
        public string? Bet { get; init; }
        public string? Bookmaker { get; init; }
        public decimal? FairOdd { get; init; }
        public decimal? Odd { get; init; }
        public decimal? Stake { get; init; }
        public bool? IsValue { get; init; }
    }
}
