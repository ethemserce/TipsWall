
namespace PreOddsApi.WebApi.V3.Contracts
{
    public sealed class ContactMessageRequest
    {
        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? Subject { get; set; }

        public string Message { get; set; } = string.Empty;

        public string? Locale { get; set; }
    }
}
