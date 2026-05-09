
namespace PreOddsApi.WebApi.V3.Contracts
{
    public sealed class RegisterDeviceRequest
    {
        public string Platform { get; set; } = string.Empty;

        public string? DeviceName { get; set; }

        public string? AppVersion { get; set; }

        public string? Locale { get; set; }

        public string? Timezone { get; set; }

        public string? PushProvider { get; set; }

        public string? PushToken { get; set; }
    }
}
