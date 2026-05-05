using System;
using Newtonsoft.Json;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class UserDto
    {
        [JsonProperty("id")]
        public Guid Id { get; init; }

        [JsonProperty("username")]
        public string? Username { get; init; }

        [JsonProperty("email")]
        public string? Email { get; init; }

        [JsonProperty("display_name")]
        public string? DisplayName { get; init; }

        [JsonProperty("role")]
        public string Role { get; init; } = "user";
    }
}
