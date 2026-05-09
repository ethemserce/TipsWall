using System;
using Microsoft.Extensions.Configuration;

namespace PreOddsApi.WebApi.V3.Auth
{
    /// <summary>
    /// Bound from the "Authentication" config section + PREODDS_JWT_SECRET env var.
    /// Single source of truth so Program.cs and AuthController don't drift.
    /// </summary>
    public sealed class AuthOptions
    {
        public const string SectionName = "Authentication";

        public string Issuer { get; set; } = "http://localhost:28332";
        public string Audience { get; set; } = "http://localhost:28332";

        /// <summary>
        /// Pulled from PREODDS_JWT_SECRET env var first, then config. Must be at
        /// least 32 chars outside Development; Program.cs enforces this at boot.
        /// </summary>
        public string JwtSecret { get; set; } = "CHANGE_ME_PREODDS_JWT_SECRET_32_CHARS_MINIMUM";

        /// <summary>
        /// Access token lifetime. Short on purpose — refresh tokens carry the
        /// long-lived session, so a stolen access token expires fast.
        /// Default: 15 minutes.
        /// </summary>
        public int AccessTokenLifetimeSeconds { get; set; } = 900;

        /// <summary>
        /// Refresh token lifetime. Default: 14 days.
        /// </summary>
        public int RefreshTokenLifetimeSeconds { get; set; } = 14 * 24 * 60 * 60;

        public static AuthOptions Load(IConfiguration configuration)
        {
            var opts = new AuthOptions();
            ConfigurationBinder.Bind(configuration.GetSection(SectionName), opts);

            var envSecret = Environment.GetEnvironmentVariable("PREODDS_JWT_SECRET");
            if (!string.IsNullOrWhiteSpace(envSecret))
            {
                opts.JwtSecret = envSecret;
            }

            return opts;
        }

        public bool IsDefaultSecret =>
            JwtSecret.StartsWith("CHANGE_ME", StringComparison.OrdinalIgnoreCase) ||
            JwtSecret.Length < 32;
    }
}
