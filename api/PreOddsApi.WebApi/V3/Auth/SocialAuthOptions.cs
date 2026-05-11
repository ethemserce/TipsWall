using Microsoft.Extensions.Configuration;

namespace PreOddsApi.WebApi.V3.Auth
{
    /// <summary>
    /// OAuth client IDs the server validates incoming Apple / Google ID
    /// tokens against. The mobile client gets its own platform-specific
    /// client IDs from Apple Developer / Google Cloud — they're sent to
    /// the user as part of the OAuth flow and surface in the JWT's
    /// `aud` claim. We verify that aud matches one of the values below
    /// so a token issued to a different app can't masquerade as ours.
    ///
    /// In Development we tolerate missing values so the server can still
    /// boot without a real Apple/Google setup; the social sign-in
    /// endpoint then returns 503 with a clear "not configured" message.
    /// Production builds without these will refuse to start.
    /// </summary>
    public sealed class SocialAuthOptions
    {
        /// <summary>Bundle id / Services id (e.g. "com.tipswall.app").</summary>
        public string AppleClientId { get; init; } = string.Empty;

        /// <summary>OAuth client_id for iOS app (Google Cloud → Credentials).</summary>
        public string GoogleClientIdIos { get; init; } = string.Empty;

        /// <summary>OAuth client_id for Android app.</summary>
        public string GoogleClientIdAndroid { get; init; } = string.Empty;

        /// <summary>OAuth client_id for web (used by expo-auth-session).</summary>
        public string GoogleClientIdWeb { get; init; } = string.Empty;

        public bool HasApple => !string.IsNullOrWhiteSpace(AppleClientId);

        public bool HasGoogle =>
            !string.IsNullOrWhiteSpace(GoogleClientIdIos) ||
            !string.IsNullOrWhiteSpace(GoogleClientIdAndroid) ||
            !string.IsNullOrWhiteSpace(GoogleClientIdWeb);

        public static SocialAuthOptions Load(IConfiguration cfg)
        {
            return new SocialAuthOptions
            {
                AppleClientId =
                    System.Environment.GetEnvironmentVariable("PREODDS_APPLE_CLIENT_ID")
                    ?? cfg["Authentication:Social:Apple:ClientId"]
                    ?? string.Empty,
                GoogleClientIdIos =
                    System.Environment.GetEnvironmentVariable("PREODDS_GOOGLE_CLIENT_ID_IOS")
                    ?? cfg["Authentication:Social:Google:ClientIdIos"]
                    ?? string.Empty,
                GoogleClientIdAndroid =
                    System.Environment.GetEnvironmentVariable("PREODDS_GOOGLE_CLIENT_ID_ANDROID")
                    ?? cfg["Authentication:Social:Google:ClientIdAndroid"]
                    ?? string.Empty,
                GoogleClientIdWeb =
                    System.Environment.GetEnvironmentVariable("PREODDS_GOOGLE_CLIENT_ID_WEB")
                    ?? cfg["Authentication:Social:Google:ClientIdWeb"]
                    ?? string.Empty,
            };
        }
    }
}
