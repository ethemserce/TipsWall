using System;
using Newtonsoft.Json;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class NotificationDto
    {
        [JsonProperty("id")]
        public Guid Id { get; init; }

        [JsonProperty("notification_type")]
        public string NotificationType { get; init; } = string.Empty;

        [JsonProperty("title")]
        public string Title { get; init; } = string.Empty;

        [JsonProperty("body")]
        public string Body { get; init; } = string.Empty;

        [JsonProperty("priority")]
        public int Priority { get; init; }

        [JsonProperty("status")]
        public string Status { get; init; } = string.Empty;

        [JsonProperty("data")]
        public string? Data { get; init; }

        [JsonProperty("scheduled_at")]
        public DateTimeOffset? ScheduledAt { get; init; }

        [JsonProperty("sent_at")]
        public DateTimeOffset? SentAt { get; init; }

        [JsonProperty("read_at")]
        public DateTimeOffset? ReadAt { get; init; }

        [JsonProperty("created_at")]
        public DateTimeOffset CreatedAt { get; init; }
    }
}
