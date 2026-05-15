using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace PreOddsApi.ExternalApis.SportMonks.Sync
{
    /// <summary>
    /// Postgres-backed scheduler. ShouldRun reads MAX(completed_at) for
    /// the job key; RecordRun inserts an audit row. Survives worker
    /// restarts — the previous in-process scheduler reset every container
    /// recreate and re-fired every job (players, transfers, fixture-backlog
    /// etc.), burning the SportMonks 3000/hr budget for no reason.
    ///
    /// In-process cache keeps the hot path cheap: ShouldRun pays one DB
    /// round-trip on first lookup per key, instant after. Write-through
    /// on RecordRun keeps the cache and the DB consistent. If the DB
    /// write fails we still update the cache so the current process
    /// doesn't re-fire the job in a hot loop; the next restart re-reads
    /// the persisted state and self-corrects.
    /// </summary>
    public sealed class PostgresSyncJobScheduler : ISyncJobScheduler
    {
        private readonly string? _connectionString;
        private readonly ILogger<PostgresSyncJobScheduler> _logger;
        private readonly ConcurrentDictionary<string, DateTimeOffset> _cache = new();

        public PostgresSyncJobScheduler(
            IConfiguration configuration,
            ILogger<PostgresSyncJobScheduler> logger)
        {
            // Same env-or-config pattern as PostgresAnalyticsEngine —
            // worker hosts pass the connection via PREODDS_POSTGRES_CONNECTION
            // env, ASP.NET host via ConnectionStrings:PreOddsApiPostgresDb.
            _connectionString =
                Environment.GetEnvironmentVariable("PREODDS_POSTGRES_CONNECTION")
                ?? configuration.GetConnectionString("PreOddsApiPostgresDb");
            _logger = logger;
        }

        public bool ShouldRun(string scheduleKey, int intervalSeconds)
        {
            var last = GetLastRun(scheduleKey);
            if (last == null)
                return true;
            return DateTimeOffset.UtcNow - last.Value >=
                   TimeSpan.FromSeconds(intervalSeconds);
        }

        public void RecordRun(string scheduleKey)
        {
            var now = DateTimeOffset.UtcNow;
            _cache[scheduleKey] = now;

            if (string.IsNullOrWhiteSpace(_connectionString))
                return;

            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                connection.Open();
                using var command = new NpgsqlCommand(
                    "insert into sync.job_runs (job_key, started_at, completed_at, status) " +
                    "values ($1, $2, $2, 'success')",
                    connection);
                command.Parameters.AddWithValue(scheduleKey);
                command.Parameters.AddWithValue(now);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                // Cache is already updated — worst case we lose this run on
                // restart. Don't take the worker down for an audit-table
                // hiccup.
                _logger.LogWarning(ex,
                    "Failed to persist sync job run for {Key}", scheduleKey);
            }
        }

        public DateTimeOffset? GetLastRunUtc(string scheduleKey)
            => GetLastRun(scheduleKey);

        private DateTimeOffset? GetLastRun(string scheduleKey)
        {
            if (_cache.TryGetValue(scheduleKey, out var cached))
                return cached;

            if (string.IsNullOrWhiteSpace(_connectionString))
                return null;

            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                connection.Open();
                using var command = new NpgsqlCommand(
                    "select max(completed_at) from sync.job_runs " +
                    "where job_key = $1 and status = 'success'",
                    connection);
                command.Parameters.AddWithValue(scheduleKey);
                var result = command.ExecuteScalar();
                if (result == null || result is DBNull)
                    return null;

                // Npgsql returns DateTime for timestamptz; the kind is
                // already UTC. Wrap into a UTC DateTimeOffset for
                // arithmetic against DateTimeOffset.UtcNow.
                var ts = (DateTime)result;
                var dto = new DateTimeOffset(
                    DateTime.SpecifyKind(ts, DateTimeKind.Utc));
                _cache[scheduleKey] = dto;
                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to load last-run for {Key}", scheduleKey);
                return null;
            }
        }
    }
}
