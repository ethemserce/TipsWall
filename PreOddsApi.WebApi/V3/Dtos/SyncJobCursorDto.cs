using System;
using Newtonsoft.Json;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class SyncJobCursorDto
    {
        [JsonProperty("job_key")]
        public string JobKey { get; init; } = string.Empty;

        [JsonProperty("entity_name")]
        public string EntityName { get; init; } = string.Empty;

        [JsonProperty("cursor_key")]
        public string? CursorKey { get; init; }

        [JsonProperty("last_success_at")]
        public DateTimeOffset? LastSuccessAt { get; init; }

        [JsonProperty("last_error_at")]
        public DateTimeOffset? LastErrorAt { get; init; }

        [JsonProperty("last_error")]
        public string? LastError { get; init; }

        [JsonProperty("has_more")]
        public bool HasMore { get; init; }

        [JsonProperty("current_page")]
        public int? CurrentPage { get; init; }
    }
}
