using System;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class DeviceDto
    {
        public Guid Id { get; init; }

        public string Platform { get; init; } = string.Empty;

        public string? DeviceName { get; init; }

        public string? AppVersion { get; init; }

        public string? Locale { get; init; }

        public string? Timezone { get; init; }

        public string? PushProvider { get; init; }

        public DateTimeOffset? LastSeenAt { get; init; }

        public DateTimeOffset CreatedAt { get; init; }
    }
}
