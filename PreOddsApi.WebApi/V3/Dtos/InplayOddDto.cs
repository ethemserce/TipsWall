using System;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class InplayOddDto
    {
        public long Id { get; init; }

        public long FixtureId { get; init; }

        public long MarketId { get; init; }

        public long BookmakerId { get; init; }

        public string OutcomeKey { get; init; } = string.Empty;

        public string Label { get; init; } = string.Empty;

        public decimal? Value { get; init; }

        public decimal? Probability { get; init; }

        public int? American { get; init; }

        public bool? Winning { get; init; }

        public bool? Suspended { get; init; }

        public bool? Stopped { get; init; }

        public string? Total { get; init; }

        public string? Handicap { get; init; }

        public DateTimeOffset CapturedAt { get; init; }

        public DateTimeOffset? LastSyncedAt { get; init; }
    }
}
