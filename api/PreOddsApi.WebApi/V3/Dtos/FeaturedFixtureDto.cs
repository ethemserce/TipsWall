using System;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class FeaturedFixtureDto
    {
        public Guid Id { get; init; }

        public long FixtureId { get; init; }

        public DateTime FeatureDate { get; init; }

        public string Source { get; init; } = string.Empty;

        public string? Title { get; init; }

        public string? Description { get; init; }

        public int Priority { get; init; }

        public bool Active { get; init; }

        public DateTimeOffset CreatedAt { get; init; }
    }
}
