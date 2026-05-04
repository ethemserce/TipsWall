using Newtonsoft.Json;

namespace PreOddsApi.WebApi.V3.Contracts
{
    public sealed class LoginRequest
    {
        [JsonProperty("username")]
        public string Username { get; set; } = string.Empty;

        [JsonProperty("password")]
        public string Password { get; set; } = string.Empty;
    }
}
