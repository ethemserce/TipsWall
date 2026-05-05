using System;
using Newtonsoft.Json;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class ApiRequestSummaryDto
    {
        [JsonProperty("id")]
        public Guid Id { get; init; }

        [JsonProperty("job_key")]
        public string? JobKey { get; init; }

        [JsonProperty("entity_name")]
        public string EntityName { get; init; } = string.Empty;

        [JsonProperty("endpoint")]
        public string Endpoint { get; init; } = string.Empty;

        [JsonProperty("status_code")]
        public int? StatusCode { get; init; }

        [JsonProperty("duration_ms")]
        public int? DurationMs { get; init; }

        [JsonProperty("started_at")]
        public DateTimeOffset StartedAt { get; init; }

        [JsonProperty("completed_at")]
        public DateTimeOffset? CompletedAt { get; init; }

        [JsonProperty("error")]
        public string? Error { get; init; }
    }
}
