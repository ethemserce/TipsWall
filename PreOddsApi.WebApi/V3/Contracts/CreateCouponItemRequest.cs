
namespace PreOddsApi.WebApi.V3.Contracts
{
    public sealed class CreateCouponItemRequest
    {
        public long FixtureId { get; set; }

        public long? OddsCurrentId { get; set; }

        public string FeedType { get; set; } = "standard";

        public long BookmakerId { get; set; }

        public long MarketId { get; set; }

        public string OutcomeKey { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;

        public decimal? OddValue { get; set; }

        public string? Total { get; set; }

        public string? Handicap { get; set; }

        public int SortOrder { get; set; }
    }
}
