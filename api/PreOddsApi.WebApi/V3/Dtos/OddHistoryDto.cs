using System;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class OddHistoryDto
    {
        public Guid Id { get; init; }

        public long? FixtureId { get; init; }

        public long? MarketId { get; init; }

        public long? BookmakerId { get; init; }

        public string? OutcomeKey { get; init; }

        public string? Label { get; init; }

        public decimal? Value { get; init; }

        public DateTimeOffset? BookmakerUpdate { get; init; }

        public DateTimeOffset CapturedAt { get; init; }
    }
}
