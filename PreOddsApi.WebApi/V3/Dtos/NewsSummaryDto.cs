using System;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class NewsSummaryDto
    {
        public long Id { get; init; }

        public long? FixtureId { get; init; }

        public long? LeagueId { get; init; }

        public string Title { get; init; } = string.Empty;

        public string? Type { get; init; }

        public DateTimeOffset CreatedAt { get; init; }
    }
}
