using System;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class ApiRequestSummaryDto
    {
        public Guid Id { get; init; }

        public string? JobKey { get; init; }

        public string EntityName { get; init; } = string.Empty;

        public string Endpoint { get; init; } = string.Empty;

        public int? StatusCode { get; init; }

        public int? DurationMs { get; init; }

        public DateTimeOffset StartedAt { get; init; }

        public DateTimeOffset? CompletedAt { get; init; }

        public string? Error { get; init; }
    }
}
