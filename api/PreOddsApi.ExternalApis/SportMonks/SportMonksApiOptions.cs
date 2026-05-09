using Microsoft.Extensions.Configuration;

namespace PreOddsApi.ExternalApis.SportMonks
{
    public sealed class SportMonksApiOptions
    {
        public const string SectionName = "SportMonks";
        public const string LegacySectionName = "SportMonksValues";
        public const string TokenEnvironmentVariable = "PREODDS_SPORTMONKS_TOKEN";

        public string BaseUrl { get; set; } = "https://api.sportmonks.com";

        public string Version { get; set; } = "v3";

        public string Sport { get; set; } = "football";

        public string? ApiToken { get; set; }

        public int DefaultPerPage { get; set; } = 50;

        public int TimeoutSeconds { get; set; } = 30;

        public int MaxRetries { get; set; } = 3;

        public bool UseAuthorizationHeader { get; set; } = true;

        public static SportMonksApiOptions FromConfiguration(IConfiguration configuration)
        {
            var options = new SportMonksApiOptions
            {
                BaseUrl = GetValue(configuration,
                    $"{SectionName}:BaseUrl",
                    $"{LegacySectionName}:api_baseUrl",
                    $"{LegacySectionName}:ApiBaseUrl") ?? "https://api.sportmonks.com",
                Version = GetValue(configuration,
                    $"{SectionName}:Version",
                    $"{LegacySectionName}:version") ?? "v3",
                Sport = GetValue(configuration,
                    $"{SectionName}:Sport",
                    $"{LegacySectionName}:sport") ?? "football",
                ApiToken = GetValue(configuration,
                    $"{SectionName}:ApiToken",
                    $"{LegacySectionName}:api_key",
                    $"{LegacySectionName}:ApiKey")
            };

            options.DefaultPerPage = GetIntValue(configuration, options.DefaultPerPage,
                $"{SectionName}:DefaultPerPage",
                $"{LegacySectionName}:default_per_page");
            options.TimeoutSeconds = GetIntValue(configuration, options.TimeoutSeconds,
                $"{SectionName}:TimeoutSeconds",
                $"{LegacySectionName}:timeout_seconds");
            options.MaxRetries = GetIntValue(configuration, options.MaxRetries,
                $"{SectionName}:MaxRetries",
                $"{LegacySectionName}:max_retries");
            options.UseAuthorizationHeader = GetBoolValue(configuration, options.UseAuthorizationHeader,
                $"{SectionName}:UseAuthorizationHeader",
                $"{LegacySectionName}:use_authorization_header");

            options.ApplyEnvironmentOverrides();
            options.Normalize();

            return options;
        }

        public void EnsureValidForRequest()
        {
            if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out _))
            {
                throw new InvalidOperationException("SportMonks BaseUrl must be an absolute URL.");
            }

            if (string.IsNullOrWhiteSpace(ApiToken))
            {
                throw new InvalidOperationException(
                    $"SportMonks API token is missing. Set {TokenEnvironmentVariable} or configure {SectionName}:ApiToken.");
            }
        }

        private void ApplyEnvironmentOverrides()
        {
            ApiToken = FirstNonEmpty(
                Environment.GetEnvironmentVariable(TokenEnvironmentVariable),
                Environment.GetEnvironmentVariable("SPORTMONKS_API_TOKEN"),
                ApiToken);

            BaseUrl = FirstNonEmpty(
                Environment.GetEnvironmentVariable("PREODDS_SPORTMONKS_BASE_URL"),
                BaseUrl) ?? BaseUrl;
        }

        private void Normalize()
        {
            BaseUrl = BaseUrl.Trim().TrimEnd('/');
            Version = Version.Trim().Trim('/');
            Sport = Sport.Trim().Trim('/');
            ApiToken = NormalizeToken(ApiToken);

            DefaultPerPage = Math.Clamp(DefaultPerPage, 1, 50);
            TimeoutSeconds = Math.Clamp(TimeoutSeconds, 5, 300);
            MaxRetries = Math.Clamp(MaxRetries, 0, 10);
        }

        private static string? NormalizeToken(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            var trimmed = token.Trim();
            return trimmed.StartsWith("CHANGE_ME", StringComparison.OrdinalIgnoreCase)
                ? null
                : trimmed;
        }

        private static string? GetValue(IConfiguration configuration, params string[] keys)
        {
            foreach (var key in keys)
            {
                var value = configuration[key];
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return null;
        }

        private static int GetIntValue(IConfiguration configuration, int defaultValue, params string[] keys)
        {
            var value = GetValue(configuration, keys);
            return int.TryParse(value, out var parsed) ? parsed : defaultValue;
        }

        private static bool GetBoolValue(IConfiguration configuration, bool defaultValue, params string[] keys)
        {
            var value = GetValue(configuration, keys);
            return bool.TryParse(value, out var parsed) ? parsed : defaultValue;
        }

        private static string? FirstNonEmpty(params string?[] values)
        {
            return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        }
    }
}
