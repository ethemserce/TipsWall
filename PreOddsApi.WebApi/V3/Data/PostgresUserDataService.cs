using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Npgsql;
using NpgsqlTypes;
using PreOddsApi.WebApi.V3.Contracts;
using PreOddsApi.WebApi.V3.Dtos;

namespace PreOddsApi.WebApi.V3.Data
{
    public sealed class PostgresUserDataService : IUserDataService
    {
        private static readonly HashSet<string> AllowedFavoriteTypes =
            new(StringComparer.OrdinalIgnoreCase) { "team", "league", "fixture" };

        private static readonly HashSet<string> AllowedOddsFormats =
            new(StringComparer.OrdinalIgnoreCase) { "decimal", "fractional", "american" };

        private readonly string? _connectionString;

        public PostgresUserDataService(IConfiguration configuration)
        {
            _connectionString = Environment.GetEnvironmentVariable("PREODDS_POSTGRES_CONNECTION")
                ?? configuration.GetConnectionString("PreOddsApiPostgresDb");
        }

        public async Task<IReadOnlyList<FavoriteDto>> GetFavoritesAsync(
            Guid userId, CancellationToken ct = default)
        {
            const string sql = """
                select id, favorite_type, team_id, league_id, fixture_id, notes, sort_order, created_at
                from app.favorites
                where user_id = @user_id
                order by favorite_type, sort_order nulls last, created_at;
                """;

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.Add(new NpgsqlParameter("user_id", userId));

            var items = new List<FavoriteDto>();
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                items.Add(MapFavorite(reader));

            return items;
        }

        public async Task<FavoriteOutcome> CreateFavoriteAsync(
            Guid userId, CreateFavoriteRequest request, CancellationToken ct = default)
        {
            var type = request.FavoriteType?.Trim().ToLowerInvariant() ?? string.Empty;
            if (!AllowedFavoriteTypes.Contains(type))
                return FavoriteOutcome.Fail(FavoriteOutcome.ErrorCodes.Validation,
                    "favorite_type must be 'team', 'league' or 'fixture'.");

            long? teamId = null, leagueId = null, fixtureId = null;
            switch (type)
            {
                case "team":
                    if (!request.TeamId.HasValue || request.TeamId.Value <= 0)
                        return FavoriteOutcome.Fail(FavoriteOutcome.ErrorCodes.Validation,
                            "team_id is required when favorite_type is 'team'.");
                    teamId = request.TeamId.Value;
                    break;
                case "league":
                    if (!request.LeagueId.HasValue || request.LeagueId.Value <= 0)
                        return FavoriteOutcome.Fail(FavoriteOutcome.ErrorCodes.Validation,
                            "league_id is required when favorite_type is 'league'.");
                    leagueId = request.LeagueId.Value;
                    break;
                case "fixture":
                    if (!request.FixtureId.HasValue || request.FixtureId.Value <= 0)
                        return FavoriteOutcome.Fail(FavoriteOutcome.ErrorCodes.Validation,
                            "fixture_id is required when favorite_type is 'fixture'.");
                    fixtureId = request.FixtureId.Value;
                    break;
            }

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(
                """
                insert into app.favorites (user_id, favorite_type, team_id, league_id, fixture_id, notes, sort_order)
                values (@user_id, @favorite_type, @team_id, @league_id, @fixture_id, @notes, @sort_order)
                returning id, favorite_type, team_id, league_id, fixture_id, notes, sort_order, created_at;
                """, connection);

            command.Parameters.Add(new NpgsqlParameter("user_id", userId));
            command.Parameters.Add(new NpgsqlParameter("favorite_type", type));
            command.Parameters.Add(new NpgsqlParameter("team_id", (object?)teamId ?? DBNull.Value));
            command.Parameters.Add(new NpgsqlParameter("league_id", (object?)leagueId ?? DBNull.Value));
            command.Parameters.Add(new NpgsqlParameter("fixture_id", (object?)fixtureId ?? DBNull.Value));
            command.Parameters.Add(new NpgsqlParameter("notes",
                (object?)(string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()) ?? DBNull.Value));
            command.Parameters.Add(new NpgsqlParameter("sort_order",
                (object?)request.SortOrder ?? DBNull.Value));

            try
            {
                await using var reader = await command.ExecuteReaderAsync(ct);
                if (await reader.ReadAsync(ct))
                    return FavoriteOutcome.Ok(MapFavorite(reader));
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                return FavoriteOutcome.Fail(FavoriteOutcome.ErrorCodes.Conflict,
                    "Favorite already exists for this user and target.");
            }

            return FavoriteOutcome.Fail(FavoriteOutcome.ErrorCodes.Validation,
                "Failed to create favorite.");
        }

        public async Task<bool> DeleteFavoriteAsync(
            Guid userId, Guid favoriteId, CancellationToken ct = default)
        {
            const string sql = "delete from app.favorites where id = @id and user_id = @user_id;";

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.Add(new NpgsqlParameter("id", favoriteId));
            command.Parameters.Add(new NpgsqlParameter("user_id", userId));

            return await command.ExecuteNonQueryAsync(ct) > 0;
        }

        public async Task<UserPreferencesDto> GetPreferencesAsync(
            Guid userId, CancellationToken ct = default)
        {
            const string sql = """
                select odds_format, locale, timezone, favorite_market_ids
                from app.user_preferences
                where user_id = @user_id
                limit 1;
                """;

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.Add(new NpgsqlParameter("user_id", userId));

            await using var reader = await command.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
                return new UserPreferencesDto { OddsFormat = "decimal" };

            return new UserPreferencesDto
            {
                OddsFormat = reader.GetString(reader.GetOrdinal("odds_format")),
                Locale = ReadNullableString(reader, "locale"),
                Timezone = ReadNullableString(reader, "timezone"),
                FavoriteMarketIds = ReadLongArray(reader, "favorite_market_ids")
            };
        }

        public async Task<PreferencesOutcome> UpsertPreferencesAsync(
            Guid userId, UpdateUserPreferencesRequest request, CancellationToken ct = default)
        {
            var oddsFormat = request.OddsFormat?.Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(oddsFormat) && !AllowedOddsFormats.Contains(oddsFormat))
                return PreferencesOutcome.Fail(FavoriteOutcome.ErrorCodes.Validation,
                    "odds_format must be 'decimal', 'fractional' or 'american'.");

            const string sql = """
                insert into app.user_preferences (user_id, odds_format, locale, timezone, favorite_market_ids)
                values (
                    @user_id,
                    coalesce(@odds_format, 'decimal'),
                    @locale,
                    @timezone,
                    @favorite_market_ids)
                on conflict (user_id) do update set
                    odds_format = coalesce(excluded.odds_format, app.user_preferences.odds_format),
                    locale = coalesce(excluded.locale, app.user_preferences.locale),
                    timezone = coalesce(excluded.timezone, app.user_preferences.timezone),
                    favorite_market_ids = coalesce(excluded.favorite_market_ids, app.user_preferences.favorite_market_ids),
                    updated_at = now()
                returning odds_format, locale, timezone, favorite_market_ids;
                """;

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.Add(new NpgsqlParameter("user_id", userId));
            command.Parameters.Add(new NpgsqlParameter("odds_format",
                (object?)oddsFormat ?? DBNull.Value));
            command.Parameters.Add(new NpgsqlParameter("locale",
                (object?)(string.IsNullOrWhiteSpace(request.Locale) ? null : request.Locale.Trim()) ?? DBNull.Value));
            command.Parameters.Add(new NpgsqlParameter("timezone",
                (object?)(string.IsNullOrWhiteSpace(request.Timezone) ? null : request.Timezone.Trim()) ?? DBNull.Value));

            var marketParam = new NpgsqlParameter("favorite_market_ids",
                NpgsqlDbType.Array | NpgsqlDbType.Bigint)
            {
                Value = (object?)request.FavoriteMarketIds?.ToArray() ?? DBNull.Value
            };
            command.Parameters.Add(marketParam);

            await using var reader = await command.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
                return PreferencesOutcome.Fail(FavoriteOutcome.ErrorCodes.Validation,
                    "Failed to save preferences.");

            return PreferencesOutcome.Ok(new UserPreferencesDto
            {
                OddsFormat = reader.GetString(reader.GetOrdinal("odds_format")),
                Locale = ReadNullableString(reader, "locale"),
                Timezone = ReadNullableString(reader, "timezone"),
                FavoriteMarketIds = ReadLongArray(reader, "favorite_market_ids")
            });
        }

        private static FavoriteDto MapFavorite(NpgsqlDataReader r) => new()
        {
            Id = r.GetGuid(r.GetOrdinal("id")),
            FavoriteType = r.GetString(r.GetOrdinal("favorite_type")),
            TeamId = ReadNullableLong(r, "team_id"),
            LeagueId = ReadNullableLong(r, "league_id"),
            FixtureId = ReadNullableLong(r, "fixture_id"),
            Notes = ReadNullableString(r, "notes"),
            SortOrder = ReadNullableInt(r, "sort_order"),
            CreatedAt = r.GetFieldValue<DateTimeOffset>(r.GetOrdinal("created_at"))
        };

        private async Task<NpgsqlConnection> OpenAsync(CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
                throw new InvalidOperationException(
                    "PostgreSQL connection string 'PreOddsApiPostgresDb' is required.");

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(ct);
            return connection;
        }

        private static long? ReadNullableLong(NpgsqlDataReader r, string column)
        {
            var i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? null : r.GetInt64(i);
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

        private static IReadOnlyList<long> ReadLongArray(NpgsqlDataReader r, string column)
        {
            var i = r.GetOrdinal(column);
            if (r.IsDBNull(i))
                return new List<long>();
            return r.GetFieldValue<long[]>(i);
        }
    }
}
