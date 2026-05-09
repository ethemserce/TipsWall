using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Npgsql;
using NpgsqlTypes;
using PreOddsApi.WebApi.V3.Dtos;

namespace PreOddsApi.WebApi.V3.Data
{
    public sealed class PostgresOddsReader : IOddsReader
    {
        private readonly NpgsqlDataSource _dataSource;

        public PostgresOddsReader(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public async Task<(IReadOnlyList<PrematchOddDto> Items, int Total)> GetPrematchOddsAsync(
            long fixtureId,
            long? bookmakerId,
            long? marketId,
            int page,
            int perPage,
            CancellationToken ct = default)
        {
            var (where, parameters) = BuildCurrentOddsWhere(fixtureId, bookmakerId, marketId);
            var sql = $"""
                select
                    id, fixture_id, market_id, bookmaker_id, outcome_key, label,
                    value, probability, american, fractional, winning, stopped,
                    total, handicap, captured_at, last_synced_at,
                    count(*) over() as total_count
                from odds.prematch_odds_current
                {where}
                order by bookmaker_id, market_id, outcome_key
                limit @limit offset @offset;
                """;

            parameters.Add(new NpgsqlParameter("limit", perPage));
            parameters.Add(new NpgsqlParameter("offset", (page - 1) * perPage));

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddRange(parameters.ToArray());

            var items = new List<PrematchOddDto>();
            var total = 0;

            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                if (total == 0)
                    total = reader.GetInt32(reader.GetOrdinal("total_count"));

                items.Add(new PrematchOddDto
                {
                    Id = reader.GetInt64(reader.GetOrdinal("id")),
                    FixtureId = reader.GetInt64(reader.GetOrdinal("fixture_id")),
                    MarketId = reader.GetInt64(reader.GetOrdinal("market_id")),
                    BookmakerId = reader.GetInt64(reader.GetOrdinal("bookmaker_id")),
                    OutcomeKey = reader.GetString(reader.GetOrdinal("outcome_key")),
                    Label = reader.GetString(reader.GetOrdinal("label")),
                    Value = ReadNullableDecimal(reader, "value"),
                    Probability = ReadNullableDecimal(reader, "probability"),
                    American = ReadNullableInt(reader, "american"),
                    Fractional = ReadNullableString(reader, "fractional"),
                    Winning = ReadNullableBool(reader, "winning"),
                    Stopped = ReadNullableBool(reader, "stopped"),
                    Total = ReadNullableString(reader, "total"),
                    Handicap = ReadNullableString(reader, "handicap"),
                    CapturedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("captured_at")),
                    LastSyncedAt = ReadNullableDateTimeOffset(reader, "last_synced_at")
                });
            }

            return (items, total);
        }

        public async Task<(IReadOnlyList<OddHistoryDto> Items, int Total)> GetPrematchHistoryAsync(
            long fixtureId,
            long? bookmakerId,
            long? marketId,
            string? outcomeKey,
            int page,
            int perPage,
            CancellationToken ct = default)
        {
            var (where, parameters) = BuildHistoryWhere(fixtureId, bookmakerId, marketId, outcomeKey);
            var sql = $"""
                select
                    id, fixture_id, market_id, bookmaker_id, outcome_key, label,
                    value, bookmaker_update, captured_at,
                    count(*) over() as total_count
                from odds.prematch_odds_history
                {where}
                order by captured_at desc
                limit @limit offset @offset;
                """;

            parameters.Add(new NpgsqlParameter("limit", perPage));
            parameters.Add(new NpgsqlParameter("offset", (page - 1) * perPage));

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddRange(parameters.ToArray());

            return await ReadHistoryAsync(command, ct);
        }

        public async Task<(IReadOnlyList<InplayOddDto> Items, int Total)> GetInplayOddsAsync(
            long fixtureId,
            long? bookmakerId,
            long? marketId,
            int page,
            int perPage,
            CancellationToken ct = default)
        {
            var (where, parameters) = BuildCurrentOddsWhere(fixtureId, bookmakerId, marketId);
            var sql = $"""
                select
                    id, fixture_id, market_id, bookmaker_id, outcome_key, label,
                    value, probability, american, winning, suspended, stopped,
                    total, handicap, captured_at, last_synced_at,
                    count(*) over() as total_count
                from odds.inplay_odds_current
                {where}
                order by bookmaker_id, market_id, outcome_key
                limit @limit offset @offset;
                """;

            parameters.Add(new NpgsqlParameter("limit", perPage));
            parameters.Add(new NpgsqlParameter("offset", (page - 1) * perPage));

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddRange(parameters.ToArray());

            var items = new List<InplayOddDto>();
            var total = 0;

            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                if (total == 0)
                    total = reader.GetInt32(reader.GetOrdinal("total_count"));

                items.Add(new InplayOddDto
                {
                    Id = reader.GetInt64(reader.GetOrdinal("id")),
                    FixtureId = reader.GetInt64(reader.GetOrdinal("fixture_id")),
                    MarketId = reader.GetInt64(reader.GetOrdinal("market_id")),
                    BookmakerId = reader.GetInt64(reader.GetOrdinal("bookmaker_id")),
                    OutcomeKey = reader.GetString(reader.GetOrdinal("outcome_key")),
                    Label = reader.GetString(reader.GetOrdinal("label")),
                    Value = ReadNullableDecimal(reader, "value"),
                    Probability = ReadNullableDecimal(reader, "probability"),
                    American = ReadNullableInt(reader, "american"),
                    Winning = ReadNullableBool(reader, "winning"),
                    Suspended = ReadNullableBool(reader, "suspended"),
                    Stopped = ReadNullableBool(reader, "stopped"),
                    Total = ReadNullableString(reader, "total"),
                    Handicap = ReadNullableString(reader, "handicap"),
                    CapturedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("captured_at")),
                    LastSyncedAt = ReadNullableDateTimeOffset(reader, "last_synced_at")
                });
            }

            return (items, total);
        }

        public async Task<(IReadOnlyList<OddHistoryDto> Items, int Total)> GetInplayHistoryAsync(
            long fixtureId,
            long? bookmakerId,
            long? marketId,
            string? outcomeKey,
            int page,
            int perPage,
            CancellationToken ct = default)
        {
            var (where, parameters) = BuildHistoryWhere(fixtureId, bookmakerId, marketId, outcomeKey);
            var sql = $"""
                select
                    id, fixture_id, market_id, bookmaker_id, outcome_key, label,
                    value, bookmaker_update, captured_at,
                    count(*) over() as total_count
                from odds.inplay_odds_history
                {where}
                order by captured_at desc
                limit @limit offset @offset;
                """;

            parameters.Add(new NpgsqlParameter("limit", perPage));
            parameters.Add(new NpgsqlParameter("offset", (page - 1) * perPage));

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddRange(parameters.ToArray());

            return await ReadHistoryAsync(command, ct);
        }

        private static (string Clause, List<NpgsqlParameter> Parameters) BuildCurrentOddsWhere(
            long fixtureId,
            long? bookmakerId,
            long? marketId)
        {
            var sb = new StringBuilder("where feed_type = 'standard' and fixture_id = @fixture_id");
            var parameters = new List<NpgsqlParameter>
            {
                new NpgsqlParameter("fixture_id", fixtureId)
            };

            if (bookmakerId.HasValue)
            {
                sb.Append(" and bookmaker_id = @bookmaker_id");
                parameters.Add(new NpgsqlParameter("bookmaker_id", bookmakerId.Value));
            }

            if (marketId.HasValue)
            {
                sb.Append(" and market_id = @market_id");
                parameters.Add(new NpgsqlParameter("market_id", marketId.Value));
            }

            return (sb.ToString(), parameters);
        }

        private static (string Clause, List<NpgsqlParameter> Parameters) BuildHistoryWhere(
            long fixtureId,
            long? bookmakerId,
            long? marketId,
            string? outcomeKey)
        {
            var sb = new StringBuilder("where fixture_id = @fixture_id");
            var parameters = new List<NpgsqlParameter>
            {
                new NpgsqlParameter("fixture_id", fixtureId)
            };

            if (bookmakerId.HasValue)
            {
                sb.Append(" and bookmaker_id = @bookmaker_id");
                parameters.Add(new NpgsqlParameter("bookmaker_id", bookmakerId.Value));
            }

            if (marketId.HasValue)
            {
                sb.Append(" and market_id = @market_id");
                parameters.Add(new NpgsqlParameter("market_id", marketId.Value));
            }

            if (!string.IsNullOrWhiteSpace(outcomeKey))
            {
                sb.Append(" and outcome_key = @outcome_key");
                parameters.Add(new NpgsqlParameter("outcome_key", outcomeKey.Trim()));
            }

            return (sb.ToString(), parameters);
        }

        private static async Task<(IReadOnlyList<OddHistoryDto> Items, int Total)> ReadHistoryAsync(
            NpgsqlCommand command,
            CancellationToken ct)
        {
            var items = new List<OddHistoryDto>();
            var total = 0;

            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                if (total == 0)
                    total = reader.GetInt32(reader.GetOrdinal("total_count"));

                items.Add(new OddHistoryDto
                {
                    Id = reader.GetGuid(reader.GetOrdinal("id")),
                    FixtureId = ReadNullableLong(reader, "fixture_id"),
                    MarketId = ReadNullableLong(reader, "market_id"),
                    BookmakerId = ReadNullableLong(reader, "bookmaker_id"),
                    OutcomeKey = ReadNullableString(reader, "outcome_key"),
                    Label = ReadNullableString(reader, "label"),
                    Value = ReadNullableDecimal(reader, "value"),
                    BookmakerUpdate = ReadNullableDateTimeOffset(reader, "bookmaker_update"),
                    CapturedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("captured_at"))
                });
            }

            return (items, total);
        }

        private Task<NpgsqlConnection> OpenAsync(CancellationToken ct)
            => _dataSource.OpenConnectionAsync(ct).AsTask();

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

        private static long? ReadNullableLong(NpgsqlDataReader r, string column)
        {
            var i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? null : r.GetInt64(i);
        }

        private static bool? ReadNullableBool(NpgsqlDataReader r, string column)
        {
            var i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? null : r.GetBoolean(i);
        }

        private static string? ReadNullableString(NpgsqlDataReader r, string column)
        {
            var i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? null : r.GetString(i);
        }

        private static DateTimeOffset? ReadNullableDateTimeOffset(NpgsqlDataReader r, string column)
        {
            var i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? null : r.GetFieldValue<DateTimeOffset>(i);
        }
    }
}
