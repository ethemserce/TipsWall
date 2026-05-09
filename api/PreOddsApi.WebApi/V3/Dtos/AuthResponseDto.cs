
namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class AuthResponseDto
    {
        public UserDto User { get; init; } = new();

        public string AccessToken { get; init; } = string.Empty;

        public string? RefreshToken { get; init; }

        public string TokenType { get; init; } = "Bearer";

        public int ExpiresIn { get; init; }
    }
}
