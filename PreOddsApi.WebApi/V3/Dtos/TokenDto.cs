
namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class TokenDto
    {
        public string AccessToken { get; init; } = string.Empty;

        public string? RefreshToken { get; init; }

        public string TokenType { get; init; } = "Bearer";

        public int ExpiresIn { get; init; }
    }
}
