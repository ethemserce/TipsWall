using Newtonsoft.Json;

namespace PreOddsApi.WebApi.V3.Contracts
{
    public sealed class ContactMessageRequest
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("email")]
        public string Email { get; set; } = string.Empty;

        [JsonProperty("subject")]
        public string? Subject { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;

        [JsonProperty("locale")]
        public string? Locale { get; set; }
    }
}
