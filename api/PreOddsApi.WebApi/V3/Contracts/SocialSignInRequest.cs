
namespace PreOddsApi.WebApi.V3.Contracts
{
    /// <summary>
    /// Mobile-supplied OAuth ID token from Apple or Google. The server
    /// verifies the signature + audience before linking the identity to
    /// an app.users row.
    /// </summary>
    public sealed class SocialSignInRequest
    {
        /// <summary>"apple" or "google".</summary>
        public string Provider { get; set; } = string.Empty;

        /// <summary>The raw JWT (id_token from the OAuth flow).</summary>
        public string IdToken { get; set; } = string.Empty;
    }
}
