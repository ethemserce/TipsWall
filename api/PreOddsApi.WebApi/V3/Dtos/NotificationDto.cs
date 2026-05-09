using System;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class NotificationDto
    {
        public Guid Id { get; init; }

        public string NotificationType { get; init; } = string.Empty;

        public string Title { get; init; } = string.Empty;

        public string Body { get; init; } = string.Empty;

        public int Priority { get; init; }

        public string Status { get; init; } = string.Empty;

        public string? Data { get; init; }

        public DateTimeOffset? ScheduledAt { get; init; }

        public DateTimeOffset? SentAt { get; init; }

        public DateTimeOffset? ReadAt { get; init; }

        public DateTimeOffset CreatedAt { get; init; }
    }
}
