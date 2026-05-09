using System;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class CouponItemDto
    {
        public Guid Id { get; init; }

        public long FixtureId { get; init; }

        public string FeedType { get; init; } = string.Empty;

        public long BookmakerId { get; init; }

        public long MarketId { get; init; }

        public string OutcomeKey { get; init; } = string.Empty;

        public string Label { get; init; } = string.Empty;

        public decimal? OddValue { get; init; }

        public string? Total { get; init; }

        public string? Handicap { get; init; }

        public string ResultStatus { get; init; } = string.Empty;

        public int SortOrder { get; init; }
    }
}
