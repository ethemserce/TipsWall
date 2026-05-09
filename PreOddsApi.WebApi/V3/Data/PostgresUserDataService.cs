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

        private static readonly HashSet<string> AllowedPlatforms =
            new(StringComparer.OrdinalIgnoreCase) { "web", "ios", "android" };

        private static readonly HashSet<string> AllowedNotificationStatuses =
            new(StringComparer.OrdinalIgnoreCase) { "pending", "sent", "read", "failed", "cancelled" };

        private readonly NpgsqlDataSource _dataSource;

        public PostgresUserDataService(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
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

        public async Task<IReadOnlyList<DeviceDto>> GetDevicesAsync(
            Guid userId, CancellationToken ct = default)
        {
            const string sql = """
                select id, platform, device_name, app_version, locale, timezone,
                       push_provider, last_seen_at, created_at
                from app.user_devices
                where user_id = @user_id and revoked_at is null
                order by last_seen_at desc nulls last, created_at desc;
                """;

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.Add(new NpgsqlParameter("user_id", userId));

            var items = new List<DeviceDto>();
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                items.Add(MapDevice(reader));

            return items;
        }

        public async Task<DeviceOutcome> RegisterDeviceAsync(
            Guid userId, RegisterDeviceRequest request, CancellationToken ct = default)
        {
            var platform = request.Platform?.Trim().ToLowerInvariant() ?? string.Empty;
            if (!AllowedPlatforms.Contains(platform))
                return DeviceOutcome.Fail("VALIDATION", "platform must be 'web', 'ios' or 'android'.");

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(
                """
                insert into app.user_devices (
                    user_id, platform, device_name, app_version, locale, timezone,
                    push_provider, push_token, last_seen_at)
                values (
                    @user_id, @platform, @device_name, @app_version, @locale, @timezone,
                    @push_provider, @push_token, now())
                returning id, platform, device_name, app_version, locale, timezone,
                          push_provider, last_seen_at, created_at;
                """, connection);

            command.Parameters.Add(new NpgsqlParameter("user_id", userId));
            command.Parameters.Add(new NpgsqlParameter("platform", platform));
            command.Parameters.Add(new NpgsqlParameter("device_name",
                (object?)NullIfEmpty(request.DeviceName) ?? DBNull.Value));
            command.Parameters.Add(new NpgsqlParameter("app_version",
                (object?)NullIfEmpty(request.AppVersion) ?? DBNull.Value));
            command.Parameters.Add(new NpgsqlParameter("locale",
                (object?)NullIfEmpty(request.Locale) ?? DBNull.Value));
            command.Parameters.Add(new NpgsqlParameter("timezone",
                (object?)NullIfEmpty(request.Timezone) ?? DBNull.Value));
            command.Parameters.Add(new NpgsqlParameter("push_provider",
                (object?)NullIfEmpty(request.PushProvider) ?? DBNull.Value));
            command.Parameters.Add(new NpgsqlParameter("push_token",
                (object?)NullIfEmpty(request.PushToken) ?? DBNull.Value));

            try
            {
                await using var reader = await command.ExecuteReaderAsync(ct);
                if (await reader.ReadAsync(ct))
                    return DeviceOutcome.Ok(MapDevice(reader));
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                return DeviceOutcome.Fail("CONFLICT",
                    "A device with the same push provider/token is already registered.");
            }

            return DeviceOutcome.Fail("VALIDATION", "Failed to register device.");
        }

        public async Task<bool> RevokeDeviceAsync(
            Guid userId, Guid deviceId, CancellationToken ct = default)
        {
            const string sql = """
                update app.user_devices
                set revoked_at = now()
                where id = @id and user_id = @user_id and revoked_at is null;
                """;

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.Add(new NpgsqlParameter("id", deviceId));
            command.Parameters.Add(new NpgsqlParameter("user_id", userId));
            return await command.ExecuteNonQueryAsync(ct) > 0;
        }

        public async Task<(IReadOnlyList<NotificationDto> Items, int Total)> GetNotificationsAsync(
            Guid userId, string? status, int page, int perPage, CancellationToken ct = default)
        {
            var clauses = new List<string> { "user_id = @user_id" };
            var parameters = new List<NpgsqlParameter>
            {
                new NpgsqlParameter("user_id", userId)
            };

            if (!string.IsNullOrWhiteSpace(status))
            {
                var s = status.Trim().ToLowerInvariant();
                if (!AllowedNotificationStatuses.Contains(s))
                    return (new List<NotificationDto>(), 0);
                clauses.Add("status = @status");
                parameters.Add(new NpgsqlParameter("status", s));
            }

            var where = "where " + string.Join(" and ", clauses);
            var sql = $"""
                select id, notification_type, title, body, priority, status,
                       data::text as data_text, scheduled_at, sent_at, read_at, created_at,
                       count(*) over() as total_count
                from app.notifications
                {where}
                order by created_at desc
                limit @limit offset @offset;
                """;

            parameters.Add(new NpgsqlParameter("limit", perPage));
            parameters.Add(new NpgsqlParameter("offset", (page - 1) * perPage));

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddRange(parameters.ToArray());

            var items = new List<NotificationDto>();
            var total = 0;

            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                if (total == 0)
                    total = reader.GetInt32(reader.GetOrdinal("total_count"));
                items.Add(MapNotification(reader));
            }

            return (items, total);
        }

        public async Task<NotificationDto?> MarkNotificationReadAsync(
            Guid userId, Guid notificationId, CancellationToken ct = default)
        {
            const string sql = """
                update app.notifications
                set status = 'read', read_at = now()
                where id = @id and user_id = @user_id and status != 'read'
                returning id, notification_type, title, body, priority, status,
                          data::text as data_text, scheduled_at, sent_at, read_at, created_at;
                """;

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.Add(new NpgsqlParameter("id", notificationId));
            command.Parameters.Add(new NpgsqlParameter("user_id", userId));

            await using var reader = await command.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
                return null;

            return MapNotification(reader);
        }

        private static DeviceDto MapDevice(NpgsqlDataReader r) => new()
        {
            Id = r.GetGuid(r.GetOrdinal("id")),
            Platform = r.GetString(r.GetOrdinal("platform")),
            DeviceName = ReadNullableString(r, "device_name"),
            AppVersion = ReadNullableString(r, "app_version"),
            Locale = ReadNullableString(r, "locale"),
            Timezone = ReadNullableString(r, "timezone"),
            PushProvider = ReadNullableString(r, "push_provider"),
            LastSeenAt = ReadNullableDateTimeOffset(r, "last_seen_at"),
            CreatedAt = r.GetFieldValue<DateTimeOffset>(r.GetOrdinal("created_at"))
        };

        private static NotificationDto MapNotification(NpgsqlDataReader r) => new()
        {
            Id = r.GetGuid(r.GetOrdinal("id")),
            NotificationType = r.GetString(r.GetOrdinal("notification_type")),
            Title = r.GetString(r.GetOrdinal("title")),
            Body = r.GetString(r.GetOrdinal("body")),
            Priority = r.GetInt32(r.GetOrdinal("priority")),
            Status = r.GetString(r.GetOrdinal("status")),
            Data = ReadNullableString(r, "data_text"),
            ScheduledAt = ReadNullableDateTimeOffset(r, "scheduled_at"),
            SentAt = ReadNullableDateTimeOffset(r, "sent_at"),
            ReadAt = ReadNullableDateTimeOffset(r, "read_at"),
            CreatedAt = r.GetFieldValue<DateTimeOffset>(r.GetOrdinal("created_at"))
        };

        private static string? NullIfEmpty(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static DateTimeOffset? ReadNullableDateTimeOffset(NpgsqlDataReader r, string column)
        {
            var i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? null : r.GetFieldValue<DateTimeOffset>(i);
        }

        private Task<NpgsqlConnection> OpenAsync(CancellationToken ct)
            => _dataSource.OpenConnectionAsync(ct).AsTask();

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
