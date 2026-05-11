using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace PreOddsApi.WebApi.V3.Auth
{
    /// <summary>
    /// Standard OpenID Connect ID-token verifier for Apple + Google.
    /// Uses ConfigurationManager + OpenIdConnectConfigurationRetriever
    /// to fetch the JWKS once per provider and refresh it transparently
    /// (defaults: refresh every 24h, retry every 5m on failure).
    /// </summary>
    public sealed class SocialTokenVerifier : ISocialTokenVerifier
    {
        private const string AppleDiscovery =
            "https://appleid.apple.com/.well-known/openid-configuration";
        private const string GoogleDiscovery =
            "https://accounts.google.com/.well-known/openid-configuration";

        private readonly SocialAuthOptions _options;
        private readonly ConfigurationManager<OpenIdConnectConfiguration> _appleConfig;
        private readonly ConfigurationManager<OpenIdConnectConfiguration> _googleConfig;
        private readonly JwtSecurityTokenHandler _handler = new();

        public SocialTokenVerifier(SocialAuthOptions options)
        {
            _options = options;
            _appleConfig = new ConfigurationManager<OpenIdConnectConfiguration>(
                AppleDiscovery, new OpenIdConnectConfigurationRetriever());
            _googleConfig = new ConfigurationManager<OpenIdConnectConfiguration>(
                GoogleDiscovery, new OpenIdConnectConfigurationRetriever());
        }

        public async Task<SocialIdentityProfile> VerifyAppleAsync(
            string idToken, CancellationToken ct = default)
        {
            if (!_options.HasApple)
                throw new SocialTokenInvalidException(
                    "Apple sign-in is not configured on this server.");

            var config = await _appleConfig.GetConfigurationAsync(ct);
            var parameters = BaseValidationParameters(config);
            parameters.ValidIssuer = "https://appleid.apple.com";
            // Apple lets a single team back multiple bundle ids → support
            // a comma-separated list in config for forward compatibility.
            parameters.ValidAudiences = SplitAudiences(_options.AppleClientId);

            return Verify(idToken, parameters);
        }

        public async Task<SocialIdentityProfile> VerifyGoogleAsync(
            string idToken, CancellationToken ct = default)
        {
            if (!_options.HasGoogle)
                throw new SocialTokenInvalidException(
                    "Google sign-in is not configured on this server.");

            var config = await _googleConfig.GetConfigurationAsync(ct);
            var parameters = BaseValidationParameters(config);
            // Google issues tokens as either of these — both are valid.
            parameters.ValidIssuers = new[]
            {
                "https://accounts.google.com",
                "accounts.google.com",
            };
            // Expo-auth-session uses the web client id; native flows use
            // iOS/Android client ids. Accept any of the three so a single
            // server handles every platform.
            var audiences = new List<string>(3);
            if (!string.IsNullOrWhiteSpace(_options.GoogleClientIdIos))
                audiences.Add(_options.GoogleClientIdIos);
            if (!string.IsNullOrWhiteSpace(_options.GoogleClientIdAndroid))
                audiences.Add(_options.GoogleClientIdAndroid);
            if (!string.IsNullOrWhiteSpace(_options.GoogleClientIdWeb))
                audiences.Add(_options.GoogleClientIdWeb);
            parameters.ValidAudiences = audiences;

            return Verify(idToken, parameters);
        }

        private static TokenValidationParameters BaseValidationParameters(
            OpenIdConnectConfiguration config)
        {
            return new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                IssuerSigningKeys = config.SigningKeys,
                // Small clock skew tolerance for mobile clients whose
                // clocks drift a few seconds.
                ClockSkew = TimeSpan.FromMinutes(2),
            };
        }

        private SocialIdentityProfile Verify(
            string idToken, TokenValidationParameters parameters)
        {
            try
            {
                _handler.ValidateToken(idToken, parameters, out var validated);
                var jwt = (JwtSecurityToken)validated;
                string Get(string name) => jwt.Payload.TryGetValue(name, out var v)
                    ? v?.ToString() ?? string.Empty
                    : string.Empty;

                var sub = Get("sub");
                if (string.IsNullOrEmpty(sub))
                    throw new SocialTokenInvalidException(
                        "ID token missing 'sub' claim.");

                var email = Get("email");
                var name = Get("name");
                var emailVerified = false;
                if (jwt.Payload.TryGetValue("email_verified", out var ev))
                    bool.TryParse(ev?.ToString(), out emailVerified);

                return new SocialIdentityProfile
                {
                    Subject = sub,
                    Email = string.IsNullOrWhiteSpace(email) ? null : email,
                    Name = string.IsNullOrWhiteSpace(name) ? null : name,
                    EmailVerified = emailVerified,
                };
            }
            catch (SocialTokenInvalidException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SocialTokenInvalidException(
                    $"ID token validation failed: {ex.Message}", ex);
            }
        }

        private static string[] SplitAudiences(string raw)
        {
            return raw.Split(',', StringSplitOptions.RemoveEmptyEntries |
                                   StringSplitOptions.TrimEntries);
        }
    }
}
