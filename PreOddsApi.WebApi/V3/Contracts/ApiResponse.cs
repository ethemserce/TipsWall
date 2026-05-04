using Newtonsoft.Json;

namespace PreOddsApi.WebApi.V3.Contracts
{
    public sealed class ApiResponse<T>
    {
        [JsonProperty("success")]
        public bool Success { get; init; }

        [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
        public T? Data { get; init; }

        [JsonProperty("pagination", NullValueHandling = NullValueHandling.Ignore)]
        public ApiPagination? Pagination { get; init; }

        [JsonProperty("error", NullValueHandling = NullValueHandling.Ignore)]
        public ApiError? Error { get; init; }

        public static ApiResponse<T> Ok(T data) => new()
        {
            Success = true,
            Data = data
        };

        public static ApiResponse<T> OkPaged(T data, ApiPagination pagination) => new()
        {
            Success = true,
            Data = data,
            Pagination = pagination
        };

        public static ApiResponse<T> Fail(string code, string message) => new()
        {
            Success = false,
            Error = new ApiError { Code = code, Message = message }
        };
    }
}
