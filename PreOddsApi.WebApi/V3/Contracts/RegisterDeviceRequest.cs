using Newtonsoft.Json;

namespace PreOddsApi.WebApi.V3.Contracts
{
    public sealed class RegisterDeviceRequest
    {
        [JsonProperty("platform")]
        public string Platform { get; set; } = string.Empty;

        [JsonProperty("device_name")]
        public string? DeviceName { get; set; }

        [JsonProperty("app_version")]
        public string? AppVersion { get; set; }

        [JsonProperty("locale")]
        public string? Locale { get; set; }

        [JsonProperty("timezone")]
        public string? Timezone { get; set; }

        [JsonProperty("push_provider")]
        public string? PushProvider { get; set; }

        [JsonProperty("push_token")]
        public string? PushToken { get; set; }
    }
}
