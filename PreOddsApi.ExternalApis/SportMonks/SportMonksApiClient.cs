using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using PreOddsApi.Entities.SportMonks;

namespace PreOddsApi.ExternalApis.SportMonks
{
    public sealed class SportMonksApiClient : ISportMonksApiClient
    {
        private static readonly Regex SecretQueryRegex = new(
            "(api_token|api_key|token)=([^&]+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly HttpClient _httpClient;
        private readonly SportMonksApiOptions _options;
        private readonly ILogger _logger;

        public SportMonksApiClient(
            HttpClient httpClient,
            SportMonksApiOptions options,
            ILogger<SportMonksApiClient> logger)
            : this(httpClient, options, (ILogger)logger)
        {
        }

        internal SportMonksApiClient(
            HttpClient httpClient,
            SportMonksApiOptions options,
            ILogger logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? NullLogger.Instance;

            ConfigureHttpClient();
        }

        public async Task<SportMonksBase<TData>> GetAsync<TData>(
            SportMonksApiRequest request,
            CancellationToken cancellationToken = default)
        {
            _options.EnsureValidForRequest();

            var uri = BuildUri(request);
            var responseBody = await SendGetAsync(uri, cancellationToken);
            var response = JsonConvert.DeserializeObject<SportMonksBase<TData>>(responseBody);

            if (response == null)
            {
                throw new SportMonksApiException(
                    "SportMonks response could not be deserialized.",
                    HttpStatusCode.OK,
                    SanitizeForLog(uri),
                    responseBody);
            }

            return response;
        }

        public async Task<TData> GetDataAsync<TData>(
            SportMonksApiRequest request,
            CancellationToken cancellationToken = default)
        {
            var response = await GetAsync<TData>(request, cancellationToken);
            return response.Data;
        }

        public async Task<IReadOnlyList<TItem>> GetAllAsync<TItem>(
            SportMonksApiRequest request,
            CancellationToken cancellationToken = default)
        {
            var items = new List<TItem>();
            var response = await GetAsync<List<TItem>>(request, cancellationToken);

            if (response.Data != null)
            {
                items.AddRange(response.Data);
            }

            var pagination = response.Pagination;
            while (pagination?.HasMore == true && pagination.NextPage != null)
            {
                var nextPageRequest = SportMonksApiRequest.Create(pagination.NextPage.ToString())
                    .WithoutDefaultPagination();

                response = await GetAsync<List<TItem>>(nextPageRequest, cancellationToken);

                if (response.Data != null)
                {
                    items.AddRange(response.Data);
                }

                pagination = response.Pagination;
            }

            return items;
        }

        private void ConfigureHttpClient()
        {
            if (_httpClient.BaseAddress == null)
            {
                _httpClient.BaseAddress = new Uri(_options.BaseUrl);
            }

            _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

            if (!_httpClient.DefaultRequestHeaders.Accept.Any(header =>
                    string.Equals(header.MediaType, "application/json", StringComparison.OrdinalIgnoreCase)))
            {
                _httpClient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
            }
        }

        private async Task<string> SendGetAsync(Uri uri, CancellationToken cancellationToken)
        {
            var attempt = 0;

            while (true)
            {
                attempt++;
                var elapsed = Stopwatch.StartNew();

                using var httpRequest = new HttpRequestMessage(HttpMethod.Get, uri);
                httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                if (_options.UseAuthorizationHeader)
                {
                    httpRequest.Headers.TryAddWithoutValidation("Authorization", _options.ApiToken);
                }

                using var response = await _httpClient.SendAsync(
                    httpRequest,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                elapsed.Stop();

                _logger.LogInformation(
                    "SportMonks GET {Endpoint} returned {StatusCode} in {ElapsedMilliseconds}ms.",
                    SanitizeForLog(uri),
                    (int)response.StatusCode,
                    elapsed.ElapsedMilliseconds);

                if (response.IsSuccessStatusCode)
                {
                    return responseBody;
                }

                if (ShouldRetry(response.StatusCode) && attempt <= _options.MaxRetries)
                {
                    var retryDelay = GetRetryDelay(response, attempt);
                    _logger.LogWarning(
                        "SportMonks GET {Endpoint} failed with {StatusCode}. Retry {Attempt}/{MaxRetries} in {DelayMilliseconds}ms.",
                        SanitizeForLog(uri),
                        (int)response.StatusCode,
                        attempt,
                        _options.MaxRetries,
                        retryDelay.TotalMilliseconds);

                    await Task.Delay(retryDelay, cancellationToken);
                    continue;
                }

                throw new SportMonksApiException(
                    $"SportMonks request failed with HTTP {(int)response.StatusCode}.",
                    response.StatusCode,
                    SanitizeForLog(uri),
                    responseBody);
            }
        }

        private Uri BuildUri(SportMonksApiRequest request)
        {
            var endpoint = request.Endpoint.Trim();

            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
            {
                var baseUri = _httpClient.BaseAddress ?? new Uri(_options.BaseUrl);
                var relativePath = BuildRelativePath(endpoint);
                uri = new Uri(baseUri, relativePath);
            }

            if (_options.UseAuthorizationHeader)
            {
                uri = RemoveSecretQueryParameters(uri);
            }

            var queryParameters = GetQueryParameters(request).ToList();
            return AppendQuery(uri, queryParameters);
        }

        private string BuildRelativePath(string endpoint)
        {
            var trimmedEndpoint = endpoint.TrimStart('/');

            if (trimmedEndpoint.StartsWith($"{_options.Version}/", StringComparison.OrdinalIgnoreCase))
            {
                return trimmedEndpoint;
            }

            return $"{_options.Version}/{_options.Sport}/{trimmedEndpoint}";
        }

        private IEnumerable<KeyValuePair<string, string>> GetQueryParameters(SportMonksApiRequest request)
        {
            foreach (var queryParameter in request.QueryParameters)
            {
                if (_options.UseAuthorizationHeader && IsSecretQueryKey(queryParameter.Key))
                {
                    continue;
                }

                yield return queryParameter;
            }

            if (request.ApplyDefaultPagination &&
                !request.QueryParameters.Any(queryParameter =>
                    string.Equals(queryParameter.Key, "per_page", StringComparison.OrdinalIgnoreCase)))
            {
                yield return new KeyValuePair<string, string>("per_page", _options.DefaultPerPage.ToString());
            }

            if (!_options.UseAuthorizationHeader &&
                !string.IsNullOrWhiteSpace(_options.ApiToken) &&
                !request.QueryParameters.Any(queryParameter =>
                    string.Equals(queryParameter.Key, "api_token", StringComparison.OrdinalIgnoreCase)))
            {
                yield return new KeyValuePair<string, string>("api_token", _options.ApiToken);
            }
        }

        private static Uri RemoveSecretQueryParameters(Uri uri)
        {
            if (string.IsNullOrWhiteSpace(uri.Query))
            {
                return uri;
            }

            var queryParts = uri.Query.TrimStart('?')
                .Split('&', StringSplitOptions.RemoveEmptyEntries);
            var filteredQueryParts = queryParts.Where(queryPart =>
            {
                var key = queryPart.Split('=', 2)[0];
                return !IsSecretQueryKey(Uri.UnescapeDataString(key));
            });

            var builder = new UriBuilder(uri)
            {
                Query = string.Join("&", filteredQueryParts)
            };

            return builder.Uri;
        }

        private static bool IsSecretQueryKey(string key)
        {
            return string.Equals(key, "api_token", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(key, "api_key", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(key, "token", StringComparison.OrdinalIgnoreCase);
        }

        private static Uri AppendQuery(Uri uri, IReadOnlyCollection<KeyValuePair<string, string>> queryParameters)
        {
            if (queryParameters.Count == 0)
            {
                return uri;
            }

            var builder = new UriBuilder(uri);
            var query = new StringBuilder(builder.Query.TrimStart('?'));

            foreach (var queryParameter in queryParameters)
            {
                if (query.Length > 0)
                {
                    query.Append('&');
                }

                query.Append(Uri.EscapeDataString(queryParameter.Key));
                query.Append('=');
                query.Append(Uri.EscapeDataString(queryParameter.Value));
            }

            builder.Query = query.ToString();
            return builder.Uri;
        }

        private static bool ShouldRetry(HttpStatusCode statusCode)
        {
            var statusCodeNumber = (int)statusCode;
            return statusCode == HttpStatusCode.TooManyRequests || statusCodeNumber >= 500;
        }

        private static TimeSpan GetRetryDelay(HttpResponseMessage response, int attempt)
        {
            if (response.Headers.RetryAfter?.Delta is { } retryAfterDelta)
            {
                return retryAfterDelta;
            }

            if (response.Headers.RetryAfter?.Date is { } retryAfterDate)
            {
                var delay = retryAfterDate - DateTimeOffset.UtcNow;
                if (delay > TimeSpan.Zero)
                {
                    return delay;
                }
            }

            return TimeSpan.FromSeconds(Math.Min(Math.Pow(2, attempt), 30));
        }

        private static string SanitizeForLog(Uri uri)
        {
            return SecretQueryRegex.Replace(uri.PathAndQuery, "$1=***");
        }
    }
}
