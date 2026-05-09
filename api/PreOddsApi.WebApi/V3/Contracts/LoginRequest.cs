
namespace PreOddsApi.WebApi.V3.Contracts
{
    public sealed class LoginRequest
    {
        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }
}
