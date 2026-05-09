using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SportMonks.Football.FixtureWorker.Services
{
    public sealed class HttpFixtureLiveBridge : IFixtureLiveBridge
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _configuration;
        private readonly ILogger<HttpFixtureLiveBridge> _logger;

        public HttpFixtureLiveBridge(
            HttpClient http,
            IConfiguration configuration,
            ILogger<HttpFixtureLiveBridge> logger)
        {
            _http = http;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task NotifyFixtureUpdatedAsync(
            long fixtureId,
            string source,
            CancellationToken cancellationToken = default)
        {
            if (!IsEnabled()) return;

            var baseUrl = (_configuration["LiveBridge:WebApiBaseUrl"] ?? string.Empty).TrimEnd('/');
            if (string.IsNullOrWhiteSpace(baseUrl)) return;

            var url = $"{baseUrl}/api/v3/internal/live/fixture/{fixtureId}/updated"
                + $"?source={Uri.EscapeDataString(source)}";

            try
            {
                using var content = new StringContent("{}", Encoding.UTF8, "application/json");
                using var response = await _http.PostAsync(url, content, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogDebug(
                        "Live bridge POST {Url} returned {Status} — push skipped.",
                        url,
                        (int)response.StatusCode);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogDebug(ex, "Live bridge POST {Url} failed; ignoring.", url);
            }
        }

        private bool IsEnabled()
        {
            var raw = _configuration["LiveBridge:Enabled"];
            return bool.TryParse(raw, out var b) ? b : false;
        }
    }
}
