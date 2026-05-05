using Newtonsoft.Json;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class TokenDto
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; init; } = string.Empty;

        [JsonProperty("refresh_token", NullValueHandling = NullValueHandling.Ignore)]
        public string? RefreshToken { get; init; }

        [JsonProperty("token_type")]
        public string TokenType { get; init; } = "Bearer";

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; init; }
    }
}
