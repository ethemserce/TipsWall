using System;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class CouponSummaryDto
    {
        public Guid Id { get; init; }

        public string PublicCode { get; init; } = string.Empty;

        public string? Title { get; init; }

        public decimal? TotalRate { get; init; }

        public string Status { get; init; } = string.Empty;

        public DateTimeOffset? StartsAt { get; init; }

        public DateTimeOffset? EndsAt { get; init; }

        public DateTimeOffset? PublishedAt { get; init; }

        public DateTimeOffset? SettledAt { get; init; }

        public DateTimeOffset CreatedAt { get; init; }
    }
}
