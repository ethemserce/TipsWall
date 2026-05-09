using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Npgsql;
using PreOddsApi.WebApi.V3.Dtos;

namespace PreOddsApi.WebApi.V3.Data
{
    public sealed class PostgresSyncDiagnostics : ISyncDiagnostics
    {
        private readonly NpgsqlDataSource _dataSource;

        public PostgresSyncDiagnostics(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public async Task<IReadOnlyList<SyncJobCursorDto>> GetSyncStatusAsync(CancellationToken ct = default)
        {
            const string sql = """
                select j.job_key, j.entity_name,
                       c.cursor_key, c.last_success_at, c.last_error_at, c.last_error,
                       c.has_more, c.current_page
                from sync.sync_jobs j
                left join sync.sync_cursors c on c.sync_job_id = j.id
                order by j.entity_name, j.job_key, c.cursor_key;
                """;

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);

            var items = new List<SyncJobCursorDto>();
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                items.Add(new SyncJobCursorDto
                {
                    JobKey = reader.GetString(reader.GetOrdinal("job_key")),
                    EntityName = reader.GetString(reader.GetOrdinal("entity_name")),
                    CursorKey = ReadNullableString(reader, "cursor_key"),
                    LastSuccessAt = ReadNullableDateTimeOffset(reader, "last_success_at"),
                    LastErrorAt = ReadNullableDateTimeOffset(reader, "last_error_at"),
                    LastError = ReadNullableString(reader, "last_error"),
                    HasMore = !reader.IsDBNull(reader.GetOrdinal("has_more"))
                        && reader.GetBoolean(reader.GetOrdinal("has_more")),
                    CurrentPage = ReadNullableInt(reader, "current_page")
                });
            }

            return items;
        }

        public async Task<IReadOnlyList<ApiRequestSummaryDto>> GetRecentRequestsAsync(
            int limit,
            CancellationToken ct = default)
        {
            const string sql = """
                select r.id, j.job_key, r.entity_name, r.endpoint, r.status_code,
                       r.duration_ms, r.started_at, r.completed_at, r.error
                from sync.api_requests r
                left join sync.sync_jobs j on j.id = r.sync_job_id
                order by r.started_at desc
                limit @limit;
                """;

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.Add(new NpgsqlParameter("limit", limit));

            var items = new List<ApiRequestSummaryDto>();
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                items.Add(new ApiRequestSummaryDto
                {
                    Id = reader.GetGuid(reader.GetOrdinal("id")),
                    JobKey = ReadNullableString(reader, "job_key"),
                    EntityName = reader.GetString(reader.GetOrdinal("entity_name")),
                    Endpoint = reader.GetString(reader.GetOrdinal("endpoint")),
                    StatusCode = ReadNullableInt(reader, "status_code"),
                    DurationMs = ReadNullableInt(reader, "duration_ms"),
                    StartedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("started_at")),
                    CompletedAt = ReadNullableDateTimeOffset(reader, "completed_at"),
                    Error = ReadNullableString(reader, "error")
                });
            }

            return items;
        }

        private Task<NpgsqlConnection> OpenAsync(CancellationToken ct)
            => _dataSource.OpenConnectionAsync(ct).AsTask();

        private static string? ReadNullableString(NpgsqlDataReader r, string column)
        {
            var i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? null : r.GetString(i);
        }

        private static int? ReadNullableInt(NpgsqlDataReader r, string column)
        {
            var i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? null : r.GetInt32(i);
        }

        private static DateTimeOffset? ReadNullableDateTimeOffset(NpgsqlDataReader r, string column)
        {
            var i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? null : r.GetFieldValue<DateTimeOffset>(i);
        }
    }
}
