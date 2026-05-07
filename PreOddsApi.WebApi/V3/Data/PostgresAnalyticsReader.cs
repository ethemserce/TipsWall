using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Npgsql;
using PreOddsApi.WebApi.V3.Dtos;

namespace PreOddsApi.WebApi.V3.Data
{
    public sealed class PostgresAnalyticsReader : IAnalyticsReader
    {
        private readonly string? _connectionString;

        public PostgresAnalyticsReader(IConfiguration configuration)
        {
            _connectionString = Environment.GetEnvironmentVariable("PREODDS_POSTGRES_CONNECTION")
                ?? configuration.GetConnectionString("PreOddsApiPostgresDb");
        }

        public Task<(IReadOnlyList<RateResultDto> Items, int Total)> GetHotRateAsync(
            long? bookmakerId, long? marketId, string? windowCode, int? matchState,
            int page, int perPage, CancellationToken ct = default)
            => QueryAsync("analytics.hot_rate_results",
                bookmakerId, marketId, windowCode, matchState, page, perPage, ct);

        public Task<(IReadOnlyList<RateResultDto> Items, int Total)> GetWinningRateAsync(
            long? bookmakerId, long? marketId, string? windowCode, int? matchState,
            int page, int perPage, CancellationToken ct = default)
            => QueryAsync("analytics.winning_rate_results",
                bookmakerId, marketId, windowCode, matchState, page, perPage, ct);

        public Task<(IReadOnlyList<RateResultDto> Items, int Total)> GetEarningRateAsync(
            long? bookmakerId, long? marketId, string? windowCode, int? matchState,
            int page, int perPage, CancellationToken ct = default)
            => QueryAsync("analytics.earning_rate_results",
                bookmakerId, marketId, windowCode, matchState, page, perPage, ct);

        private async Task<(IReadOnlyList<RateResultDto> Items, int Total)> QueryAsync(
            string tableName,
            long? bookmakerId, long? marketId, string? windowCode, int? matchState,
            int page, int perPage, CancellationToken ct)
        {
            var clauses = new List<string>();
            var parameters = new List<NpgsqlParameter>();

            if (bookmakerId.HasValue)
            {
                clauses.Add("bookmaker_id = @bookmaker_id");
                parameters.Add(new NpgsqlParameter("bookmaker_id", bookmakerId.Value));
            }
            if (marketId.HasValue)
            {
                clauses.Add("market_id = @market_id");
                parameters.Add(new NpgsqlParameter("market_id", marketId.Value));
            }
            if (!string.IsNullOrWhiteSpace(windowCode))
            {
                clauses.Add("window_code = @window_code");
                parameters.Add(new NpgsqlParameter("window_code", windowCode.Trim()));
            }
            if (matchState.HasValue)
            {
                clauses.Add("match_state = @match_state");
                parameters.Add(new NpgsqlParameter("match_state", matchState.Value));
            }

            var where = clauses.Count > 0 ? "where " + string.Join(" and ", clauses) : string.Empty;

            var sql = $"""
                select id, fixture_id, fixture_signal_id, bookmaker_id, market_id,
                       window_code, outcome_key, label, odd_value, total, handicap,
                       win_count, lost_count, sample_count,
                       winning_percent, earning_percent, rank_order, match_state,
                       count(*) over() as total_count
                from {tableName}
                {where}
                order by bookmaker_id, market_id, window_code, rank_order
                limit @limit offset @offset;
                """;

            parameters.Add(new NpgsqlParameter("limit", perPage));
            parameters.Add(new NpgsqlParameter("offset", (page - 1) * perPage));

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddRange(parameters.ToArray());

            var items = new List<RateResultDto>();
            var total = 0;

            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                if (total == 0)
                    total = reader.GetInt32(reader.GetOrdinal("total_count"));

                items.Add(new RateResultDto
                {
                    Id = reader.GetGuid(reader.GetOrdinal("id")),
                    FixtureId = reader.GetInt64(reader.GetOrdinal("fixture_id")),
                    FixtureSignalId = ReadNullableGuid(reader, "fixture_signal_id"),
                    BookmakerId = reader.GetInt64(reader.GetOrdinal("bookmaker_id")),
                    MarketId = reader.GetInt64(reader.GetOrdinal("market_id")),
                    WindowCode = reader.GetString(reader.GetOrdinal("window_code")),
                    OutcomeKey = reader.GetString(reader.GetOrdinal("outcome_key")),
                    Label = reader.GetString(reader.GetOrdinal("label")),
                    OddValue = ReadNullableDecimal(reader, "odd_value"),
                    Total = ReadNullableString(reader, "total"),
                    Handicap = ReadNullableString(reader, "handicap"),
                    WinCount = reader.GetInt32(reader.GetOrdinal("win_count")),
                    LostCount = reader.GetInt32(reader.GetOrdinal("lost_count")),
                    SampleCount = reader.GetInt32(reader.GetOrdinal("sample_count")),
                    WinningPercent = ReadNullableDecimal(reader, "winning_percent"),
                    EarningPercent = ReadNullableDecimal(reader, "earning_percent"),
                    RankOrder = reader.GetInt32(reader.GetOrdinal("rank_order")),
                    MatchState = ReadNullableInt(reader, "match_state")
                });
            }

            return (items, total);
        }

        private async Task<NpgsqlConnection> OpenAsync(CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
                throw new InvalidOperationException(
                    "PostgreSQL connection string 'PreOddsApiPostgresDb' is required.");

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(ct);
            return connection;
        }

        private static Guid? ReadNullableGuid(NpgsqlDataReader r, string column)
        {
            var i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? null : r.GetGuid(i);
        }

        private static decimal? ReadNullableDecimal(NpgsqlDataReader r, string column)
        {
            var i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? null : r.GetDecimal(i);
        }

        private static int? ReadNullableInt(NpgsqlDataReader r, string column)
        {
            var i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? null : r.GetInt32(i);
        }

        private static string? ReadNullableString(NpgsqlDataReader r, string column)
        {
            var i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? null : r.GetString(i);
        }
    }
}
