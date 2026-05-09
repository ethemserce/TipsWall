using System;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class SyncJobCursorDto
    {
        public string JobKey { get; init; } = string.Empty;

        public string EntityName { get; init; } = string.Empty;

        public string? CursorKey { get; init; }

        public DateTimeOffset? LastSuccessAt { get; init; }

        public DateTimeOffset? LastErrorAt { get; init; }

        public string? LastError { get; init; }

        public bool HasMore { get; init; }

        public int? CurrentPage { get; init; }
    }
}
