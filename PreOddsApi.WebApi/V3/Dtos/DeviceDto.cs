using System;
using Newtonsoft.Json;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class DeviceDto
    {
        [JsonProperty("id")]
        public Guid Id { get; init; }

        [JsonProperty("platform")]
        public string Platform { get; init; } = string.Empty;

        [JsonProperty("device_name")]
        public string? DeviceName { get; init; }

        [JsonProperty("app_version")]
        public string? AppVersion { get; init; }

        [JsonProperty("locale")]
        public string? Locale { get; init; }

        [JsonProperty("timezone")]
        public string? Timezone { get; init; }

        [JsonProperty("push_provider")]
        public string? PushProvider { get; init; }

        [JsonProperty("last_seen_at")]
        public DateTimeOffset? LastSeenAt { get; init; }

        [JsonProperty("created_at")]
        public DateTimeOffset CreatedAt { get; init; }
    }
}
