using System;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class FavoriteDto
    {
        public Guid Id { get; init; }

        public string FavoriteType { get; init; } = string.Empty;

        public long? TeamId { get; init; }

        public long? LeagueId { get; init; }

        public long? FixtureId { get; init; }

        public string? Notes { get; init; }

        public int? SortOrder { get; init; }

        public DateTimeOffset CreatedAt { get; init; }
    }
}
