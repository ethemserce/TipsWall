using Newtonsoft.Json;

namespace PreOddsApi.WebApi.V3.Contracts
{
    public sealed class ApiError
    {
        [JsonProperty("code")]
        public string Code { get; init; } = string.Empty;

        [JsonProperty("message")]
        public string Message { get; init; } = string.Empty;

        public static class Codes
        {
            public const string NotFound = "NOT_FOUND";
            public const string BadRequest = "BAD_REQUEST";
            public const string Unauthorized = "UNAUTHORIZED";
            public const string Forbidden = "FORBIDDEN";
            public const string InternalError = "INTERNAL_ERROR";
        }
    }
}
