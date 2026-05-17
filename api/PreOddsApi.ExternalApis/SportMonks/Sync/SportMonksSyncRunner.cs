using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;

namespace PreOddsApi.ExternalApis.SportMonks.Sync
{
    public sealed class SportMonksSyncRunner : ISportMonksSyncRunner
    {
        private static readonly Regex SecretQueryRegex = new(
            "(api_token|api_key|token)=([^&]+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly ISportMonksApiClient _apiClient;
        private readonly SportMonksApiOptions _apiOptions;
        private readonly ILogger<SportMonksSyncRunner> _logger;
        private readonly string? _connectionString;
        // Off-switch for the debug-only raw_payloads archival. SportMonks
        // responses can be multi-MB JSON; bulk inserts into sync.raw_payloads
        // were the smoking gun for the Postgres OOM-kill / recovery cycles
        // seen 2026-05-17 (single INSERT pushed the DB over its memory
        // limit, Linux SIGKILL'd the backend process, all live + pulse
        // ticks failed with 57P03 during the recovery window). Override
        // with env var `SportMonksSync__StoreRawPayloads=false` to skip
        // the insert entirely. Default stays true so dev / staging still
        // captures payloads for audit.
        private readonly bool _storeRawPayloads;

        public SportMonksSyncRunner(
            ISportMonksApiClient apiClient,
            SportMonksApiOptions apiOptions,
            IConfiguration configuration,
            ILogger<SportMonksSyncRunner> logger)
        {
            _apiClient = apiClient;
            _apiOptions = apiOptions;
            _logger = logger;
            _connectionString = Environment.GetEnvironmentVariable("PREODDS_POSTGRES_CONNECTION")
                ?? configuration.GetConnectionString("PreOddsApiPostgresDb");
            _storeRawPayloads = configuration.GetValue("SportMonksSync:StoreRawPayloads", true);
        }

        public async Task<IReadOnlyList<TItem>> GetAllAsync<TItem>(
            SportMonksSyncJobDefinition jobDefinition,
            SportMonksApiRequest request,
            string? cursorKey = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(jobDefinition);
            ArgumentNullException.ThrowIfNull(request);

            var jobId = await EnsureJobAsync(jobDefinition, cancellationToken);
            var items = new List<TItem>();
            var currentRequest = request;
            var resolvedCursorKey = cursorKey ?? GetCursorKey(request);

            while (true)
            {
                var requestUrl = BuildRequestUrl(currentRequest);
                var endpoint = GetEndpointForStorage(currentRequest);
                var queryHash = ComputeQueryHash(requestUrl);
                var apiRequestId = await StartApiRequestAsync(
                    jobId,
                    jobDefinition,
                    endpoint,
                    requestUrl,
                    cancellationToken);
                var stopwatch = Stopwatch.StartNew();

                try
                {
                    var response = await _apiClient.GetAsync<List<TItem>>(currentRequest, cancellationToken);
                    stopwatch.Stop();

                    if (response.Data != null)
                    {
                        items.AddRange(response.Data);
                    }

                    await StoreRawPayloadAsync(
                        jobDefinition,
                        endpoint,
                        requestUrl,
                        response,
                        cancellationToken);

                    await CompleteApiRequestAsync(
                        apiRequestId,
                        HttpStatusCode.OK,
                        stopwatch.Elapsed,
                        null,
                        cancellationToken);

                    await UpsertCursorSuccessAsync(
                        jobId,
                        resolvedCursorKey,
                        requestUrl,
                        queryHash,
                        response.Pagination?.NextPage?.ToString(),
                        response.Pagination?.HasMore ?? false,
                        response.Pagination?.CurrentPage,
                        cancellationToken);

                    if (response.Pagination?.HasMore == true && response.Pagination.NextPage != null)
                    {
                        if (request.RequestDelayMs > 0)
                            await Task.Delay(request.RequestDelayMs, cancellationToken);

                        currentRequest = SportMonksApiRequest.Create(response.Pagination.NextPage.ToString())
                            .WithoutDefaultPagination();
                        continue;
                    }

                    break;
                }
                catch (SportMonksApiException exception)
                {
                    stopwatch.Stop();
                    await CompleteApiRequestAsync(
                        apiRequestId,
                        exception.StatusCode,
                        stopwatch.Elapsed,
                        exception.Message,
                        cancellationToken);
                    await UpsertCursorFailureAsync(
                        jobId,
                        resolvedCursorKey,
                        requestUrl,
                        queryHash,
                        exception.Message,
                        cancellationToken);
                    throw;
                }
                catch (Exception exception)
                {
                    stopwatch.Stop();
                    await CompleteApiRequestAsync(
                        apiRequestId,
                        null,
                        stopwatch.Elapsed,
                        exception.Message,
                        cancellationToken);
                    await UpsertCursorFailureAsync(
                        jobId,
                        resolvedCursorKey,
                        requestUrl,
                        queryHash,
                        exception.Message,
                        cancellationToken);
                    throw;
                }
            }

            _logger.LogInformation(
                "SportMonks sync job {JobKey} completed with {ItemCount} items.",
                jobDefinition.JobKey,
                items.Count);

            return items;
        }

        private async Task<Guid> EnsureJobAsync(
            SportMonksSyncJobDefinition jobDefinition,
            CancellationToken cancellationToken)
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                insert into sync.sync_jobs (job_key, provider, entity_name, description, schedule)
                values (@job_key, @provider, @entity_name, @description, @schedule)
                on conflict (job_key) do update set
                    provider = excluded.provider,
                    entity_name = excluded.entity_name,
                    description = excluded.description,
                    schedule = excluded.schedule,
                    updated_at = now()
                returning id;
                """;
            command.Parameters.Add(Parameter("job_key", jobDefinition.JobKey));
            command.Parameters.Add(Parameter("provider", jobDefinition.Provider));
            command.Parameters.Add(Parameter("entity_name", jobDefinition.EntityName));
            command.Parameters.Add(Parameter("description", jobDefinition.Description));
            command.Parameters.Add(Parameter("schedule", jobDefinition.Schedule));

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return (Guid)result!;
        }

        private async Task<Guid> StartApiRequestAsync(
            Guid jobId,
            SportMonksSyncJobDefinition jobDefinition,
            string endpoint,
            string requestUrl,
            CancellationToken cancellationToken)
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                insert into sync.api_requests (sync_job_id, provider, entity_name, endpoint, request_url)
                values (@sync_job_id, @provider, @entity_name, @endpoint, @request_url)
                returning id;
                """;
            command.Parameters.Add(Parameter("sync_job_id", jobId));
            command.Parameters.Add(Parameter("provider", jobDefinition.Provider));
            command.Parameters.Add(Parameter("entity_name", jobDefinition.EntityName));
            command.Parameters.Add(Parameter("endpoint", endpoint));
            command.Parameters.Add(Parameter("request_url", requestUrl));

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return (Guid)result!;
        }

        private async Task CompleteApiRequestAsync(
            Guid apiRequestId,
            HttpStatusCode? statusCode,
            TimeSpan duration,
            string? error,
            CancellationToken cancellationToken)
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                update sync.api_requests
                set status_code = @status_code,
                    duration_ms = @duration_ms,
                    completed_at = now(),
                    error = @error
                where id = @id;
                """;
            command.Parameters.Add(Parameter("id", apiRequestId));
            command.Parameters.Add(Parameter("status_code", statusCode.HasValue ? (int)statusCode.Value : null));
            command.Parameters.Add(Parameter("duration_ms", Convert.ToInt32(duration.TotalMilliseconds)));
            command.Parameters.Add(Parameter("error", Truncate(error, 4000)));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private async Task StoreRawPayloadAsync(
            SportMonksSyncJobDefinition jobDefinition,
            string endpoint,
            string requestUrl,
            object payload,
            CancellationToken cancellationToken)
        {
            // Skip the insert entirely when archival is disabled. The
            // serialize step alone can allocate tens of MB on a large
            // fixtures-between response, so we short-circuit before the
            // allocation rather than after.
            if (!_storeRawPayloads) return;

            var payloadJson = JsonConvert.SerializeObject(payload);

            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                insert into sync.raw_payloads (provider, entity_name, endpoint, request_url, payload)
                values (@provider, @entity_name, @endpoint, @request_url, @payload);
                """;
            command.Parameters.Add(Parameter("provider", jobDefinition.Provider));
            command.Parameters.Add(Parameter("entity_name", jobDefinition.EntityName));
            command.Parameters.Add(Parameter("endpoint", endpoint));
            command.Parameters.Add(Parameter("request_url", requestUrl));
            command.Parameters.Add(new NpgsqlParameter("payload", NpgsqlDbType.Jsonb)
            {
                Value = payloadJson
            });

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private async Task UpsertCursorSuccessAsync(
            Guid jobId,
            string cursorKey,
            string requestUrl,
            string queryHash,
            string? nextPage,
            bool hasMore,
            long? currentPage,
            CancellationToken cancellationToken)
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                insert into sync.sync_cursors (
                    sync_job_id,
                    cursor_key,
                    request_url,
                    query_hash,
                    next_page,
                    has_more,
                    current_page,
                    last_success_at)
                values (
                    @sync_job_id,
                    @cursor_key,
                    @request_url,
                    @query_hash,
                    @next_page,
                    @has_more,
                    @current_page,
                    now())
                on conflict (sync_job_id, cursor_key) do update set
                    request_url = excluded.request_url,
                    query_hash = excluded.query_hash,
                    next_page = excluded.next_page,
                    has_more = excluded.has_more,
                    current_page = excluded.current_page,
                    last_success_at = now(),
                    last_error = null,
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("sync_job_id", jobId));
            command.Parameters.Add(Parameter("cursor_key", cursorKey));
            command.Parameters.Add(Parameter("request_url", requestUrl));
            command.Parameters.Add(Parameter("query_hash", queryHash));
            command.Parameters.Add(Parameter("next_page", SanitizeForStorage(nextPage)));
            command.Parameters.Add(Parameter("has_more", hasMore));
            command.Parameters.Add(Parameter("current_page", currentPage.HasValue ? Convert.ToInt32(currentPage.Value) : null));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private async Task UpsertCursorFailureAsync(
            Guid jobId,
            string cursorKey,
            string requestUrl,
            string queryHash,
            string error,
            CancellationToken cancellationToken)
        {
            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                insert into sync.sync_cursors (
                    sync_job_id,
                    cursor_key,
                    request_url,
                    query_hash,
                    has_more,
                    last_error_at,
                    last_error)
                values (
                    @sync_job_id,
                    @cursor_key,
                    @request_url,
                    @query_hash,
                    false,
                    now(),
                    @last_error)
                on conflict (sync_job_id, cursor_key) do update set
                    request_url = excluded.request_url,
                    query_hash = excluded.query_hash,
                    has_more = false,
                    last_error_at = now(),
                    last_error = excluded.last_error,
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("sync_job_id", jobId));
            command.Parameters.Add(Parameter("cursor_key", cursorKey));
            command.Parameters.Add(Parameter("request_url", requestUrl));
            command.Parameters.Add(Parameter("query_hash", queryHash));
            command.Parameters.Add(Parameter("last_error", Truncate(error, 4000)));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                throw new InvalidOperationException(
                    "PostgreSQL connection string 'PreOddsApiPostgresDb' is required for SportMonks sync tracking.");
            }

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }

        private string BuildRequestUrl(SportMonksApiRequest request)
        {
            var endpoint = request.Endpoint.Trim();

            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
            {
                var baseUri = new Uri(_apiOptions.BaseUrl);
                var relativePath = BuildRelativePath(endpoint);
                uri = new Uri(baseUri, relativePath);
            }

            var queryParameters = GetQueryParametersForStorage(request).ToList();
            uri = AppendQuery(uri, queryParameters);

            return SanitizeForStorage(uri.ToString());
        }

        private string BuildRelativePath(string endpoint)
        {
            var trimmedEndpoint = endpoint.TrimStart('/');

            if (trimmedEndpoint.StartsWith($"{_apiOptions.Version}/", StringComparison.OrdinalIgnoreCase))
            {
                return trimmedEndpoint;
            }

            return $"{_apiOptions.Version}/{_apiOptions.Sport}/{trimmedEndpoint}";
        }

        private IEnumerable<KeyValuePair<string, string>> GetQueryParametersForStorage(SportMonksApiRequest request)
        {
            foreach (var queryParameter in request.QueryParameters)
            {
                if (IsSecretQueryKey(queryParameter.Key))
                {
                    continue;
                }

                yield return queryParameter;
            }

            if (request.ApplyDefaultPagination &&
                !request.QueryParameters.Any(queryParameter =>
                    string.Equals(queryParameter.Key, "per_page", StringComparison.OrdinalIgnoreCase)))
            {
                yield return new KeyValuePair<string, string>("per_page", _apiOptions.DefaultPerPage.ToString());
            }
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

        private static string GetEndpointForStorage(SportMonksApiRequest request)
        {
            return Uri.TryCreate(request.Endpoint, UriKind.Absolute, out var uri)
                ? uri.AbsolutePath.TrimStart('/')
                : request.Endpoint.Split('?', 2)[0].TrimStart('/');
        }

        private static string GetCursorKey(SportMonksApiRequest request)
        {
            return GetEndpointForStorage(request);
        }

        private static string ComputeQueryHash(string value)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private static bool IsSecretQueryKey(string key)
        {
            return string.Equals(key, "api_token", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(key, "api_key", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(key, "token", StringComparison.OrdinalIgnoreCase);
        }

        private static string SanitizeForStorage(string? value)
        {
            return SecretQueryRegex.Replace(value ?? string.Empty, "$1=***");
        }

        private static string? Truncate(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            return value.Length <= maxLength
                ? value
                : value[..maxLength];
        }

        private static NpgsqlParameter Parameter(string name, object? value)
        {
            return new NpgsqlParameter(name, value ?? DBNull.Value);
        }
    }
}
