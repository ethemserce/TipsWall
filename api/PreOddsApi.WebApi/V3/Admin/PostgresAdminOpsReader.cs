using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace PreOddsApi.WebApi.V3.Admin
{
    public sealed class PostgresAdminOpsReader : IAdminOpsReader
    {
        private readonly string? _connectionString;

        public PostgresAdminOpsReader(IConfiguration configuration)
        {
            _connectionString = Environment.GetEnvironmentVariable("PREODDS_POSTGRES_CONNECTION")
                ?? configuration.GetConnectionString("PreOddsApiPostgresDb");
        }

        public async Task<IReadOnlyList<WorkerTierStatusDto>> GetWorkerTierStatusAsync(CancellationToken ct)
        {
            // sync.job_runs ships a row per execution; per-job last-run /
            // last-failure + total runs come from a single pass with
            // window aggregates. Newest job_key first so the dashboard
            // surfaces actively-ticking tiers at the top.
            const string sql = """
                select
                    job_key,
                    max(run_at) as last_run_at,
                    max(run_at) filter (where succeeded = false) as last_failure_at,
                    (array_agg(error_message order by run_at desc) filter (where succeeded = false))[1] as last_error,
                    count(*) as run_count
                from sync.job_runs
                where run_at > now() - interval '24 hours'
                group by job_key
                order by max(run_at) desc;
                """;

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);

            var results = new List<WorkerTierStatusDto>();
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                results.Add(new WorkerTierStatusDto
                {
                    JobKey = reader.GetString(reader.GetOrdinal("job_key")),
                    LastRunAt = ReadNullableDateTimeOffset(reader, "last_run_at"),
                    LastFailureAt = ReadNullableDateTimeOffset(reader, "last_failure_at"),
                    LastError = TruncateError(ReadNullableString(reader, "last_error")),
                    RunCount = reader.GetInt64(reader.GetOrdinal("run_count"))
                });
            }
            return results;
        }

        public async Task<PostgresHealthDto> GetPostgresHealthAsync(CancellationToken ct)
        {
            // Two-pass against pg_stat_activity: the first scalar query
            // pulls the headline metrics, the second returns the slow-
            // query list. Sharing one connection keeps the round-trip
            // count down.
            const string headlineSql = """
                with active_backends as (
                    select pid, query, now() - query_start as runtime
                    from pg_stat_activity
                    where state = 'active' and pid != pg_backend_pid()
                ),
                longest as (
                    select extract(epoch from runtime)::float as runtime_seconds,
                           left(query, 120) as query_text
                    from active_backends
                    order by runtime desc nulls last
                    limit 1
                )
                select
                    (select count(*) from active_backends) as active_queries,
                    (select count(*) from pg_stat_activity where pid != pg_backend_pid()) as total_connections,
                    (select runtime_seconds from longest) as longest_query_seconds,
                    (select query_text from longest) as longest_query_text,
                    pg_is_in_recovery() as in_recovery,
                    pg_database_size(current_database()) as database_bytes;
                """;
            // Slow-query list — anything that has been running for more
            // than @threshold_seconds. Bounded to 10 rows so a flood
            // doesn't bloat the response.
            const string slowSql = """
                select pid,
                       extract(epoch from now() - query_start)::float as duration_seconds,
                       left(query, 200) as query_text,
                       state
                from pg_stat_activity
                where state = 'active'
                  and pid != pg_backend_pid()
                  and now() - query_start > make_interval(secs => @threshold_seconds)
                order by query_start
                limit 10;
                """;

            await using var connection = await OpenAsync(ct);

            int activeQueries;
            int totalConnections;
            double? longestQuerySeconds;
            string? longestQueryText;
            bool inRecovery;
            long databaseBytes;
            await using (var command = new NpgsqlCommand(headlineSql, connection))
            await using (var reader = await command.ExecuteReaderAsync(ct))
            {
                if (!await reader.ReadAsync(ct))
                {
                    return new PostgresHealthDto { DiskFreeBytes = 0, DiskTotalBytes = 0 };
                }
                activeQueries = reader.GetInt32(reader.GetOrdinal("active_queries"));
                totalConnections = reader.GetInt32(reader.GetOrdinal("total_connections"));
                longestQuerySeconds = ReadNullableDouble(reader, "longest_query_seconds");
                longestQueryText = ReadNullableString(reader, "longest_query_text");
                inRecovery = reader.GetBoolean(reader.GetOrdinal("in_recovery"));
                databaseBytes = reader.GetInt64(reader.GetOrdinal("database_bytes"));
            }

            var slowQueries = new List<SlowQueryDto>();
            await using (var command = new NpgsqlCommand(slowSql, connection))
            {
                command.Parameters.AddWithValue("threshold_seconds", 60);
                await using var reader = await command.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    slowQueries.Add(new SlowQueryDto
                    {
                        Pid = reader.GetInt32(reader.GetOrdinal("pid")),
                        DurationSeconds = reader.GetDouble(reader.GetOrdinal("duration_seconds")),
                        Query = ReadNullableString(reader, "query_text") ?? string.Empty,
                        State = ReadNullableString(reader, "state") ?? string.Empty,
                    });
                }
            }

            // Disk metric for the postgres data mount. On our Docker
            // setup postgres runs in the same container hierarchy as the
            // WebAPI host filesystem (overlay2 over the host root), so
            // DriveInfo('/') reflects the same fill level the user sees
            // in `df -h /` on the host. Falls back to zero on Windows /
            // platforms where '/' isn't a valid path.
            long diskFree = 0;
            long diskTotal = 0;
            try
            {
                var drive = new DriveInfo("/");
                if (drive.IsReady)
                {
                    diskFree = drive.AvailableFreeSpace;
                    diskTotal = drive.TotalSize;
                }
            }
            catch { /* non-Linux host — leave zeros */ }

            return new PostgresHealthDto
            {
                ActiveQueries = activeQueries,
                TotalConnections = totalConnections,
                LongestQuerySeconds = longestQuerySeconds,
                LongestQueryText = longestQueryText,
                InRecovery = inRecovery,
                DatabaseBytes = databaseBytes,
                DiskFreeBytes = diskFree,
                DiskTotalBytes = diskTotal,
                DiskUsedPercent = diskTotal > 0
                    ? Math.Round((double)(diskTotal - diskFree) / diskTotal * 100.0, 2)
                    : 0,
                SlowQueries = slowQueries,
            };
        }

        private async Task<NpgsqlConnection> OpenAsync(CancellationToken ct)
        {
            var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(ct);
            return conn;
        }

        private static string? ReadNullableString(NpgsqlDataReader reader, string column)
        {
            var i = reader.GetOrdinal(column);
            return reader.IsDBNull(i) ? null : reader.GetString(i);
        }

        private static DateTimeOffset? ReadNullableDateTimeOffset(NpgsqlDataReader reader, string column)
        {
            var i = reader.GetOrdinal(column);
            if (reader.IsDBNull(i)) return null;
            var raw = reader.GetFieldValue<object>(i);
            return raw switch
            {
                DateTimeOffset dto => dto,
                DateTime dt => new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc)),
                _ => null
            };
        }

        private static double? ReadNullableDouble(NpgsqlDataReader reader, string column)
        {
            var i = reader.GetOrdinal(column);
            return reader.IsDBNull(i) ? null : reader.GetDouble(i);
        }

        private static string? TruncateError(string? error)
        {
            if (string.IsNullOrWhiteSpace(error)) return null;
            return error.Length > 200 ? error[..200] : error;
        }
    }
}
