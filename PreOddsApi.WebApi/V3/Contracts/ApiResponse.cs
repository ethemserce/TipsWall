
namespace PreOddsApi.WebApi.V3.Contracts
{
    public sealed class ApiResponse<T>
    {
        public bool Success { get; init; }

        public T? Data { get; init; }

        public ApiPagination? Pagination { get; init; }

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
