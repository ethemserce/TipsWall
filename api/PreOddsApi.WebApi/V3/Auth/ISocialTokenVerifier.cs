using System.Threading;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.V3.Auth
{
    /// <summary>
    /// Verifies an Apple / Google ID token: signature against the
    /// provider's JWKS, issuer + audience claims against our config,
    /// expiry vs. wall clock. Returns the verified profile when good,
    /// throws SocialTokenInvalidException otherwise.
    /// </summary>
    public interface ISocialTokenVerifier
    {
        Task<SocialIdentityProfile> VerifyAppleAsync(
            string idToken,
            CancellationToken ct = default);

        Task<SocialIdentityProfile> VerifyGoogleAsync(
            string idToken,
            CancellationToken ct = default);
    }

    public sealed class SocialIdentityProfile
    {
        public string Subject { get; init; } = string.Empty;
        public string? Email { get; init; }
        public string? Name { get; init; }
        public bool EmailVerified { get; init; }
    }

    public sealed class SocialTokenInvalidException : System.Exception
    {
        public SocialTokenInvalidException(string message) : base(message) { }
        public SocialTokenInvalidException(string message, System.Exception inner)
            : base(message, inner) { }
    }
}
