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
    public sealed class PostgresAppSchemaService : IAppSchemaService
    {
        private readonly string? _connectionString;

        public PostgresAppSchemaService(IConfiguration configuration)
        {
            _connectionString = Environment.GetEnvironmentVariable("PREODDS_POSTGRES_CONNECTION")
                ?? configuration.GetConnectionString("PreOddsApiPostgresDb");
        }

        public async Task<(IReadOnlyList<FeaturedFixtureDto> Items, int Total)> GetFeaturedFixturesAsync(
            DateTime? featureDate,
            int page,
            int perPage,
            CancellationToken ct = default)
        {
            var sb = new StringBuilder("where active = true");
            var parameters = new List<NpgsqlParameter>();

            if (featureDate.HasValue)
            {
                sb.Append(" and feature_date = @feature_date");
                parameters.Add(new NpgsqlParameter("feature_date", featureDate.Value.Date));
            }

            var sql = $"""
                select id, fixture_id, feature_date, source, title, description,
                       priority, active, created_at,
                       count(*) over() as total_count
                from app.featured_fixtures
                {sb}
                order by feature_date desc, priority desc, created_at desc
                limit @limit offset @offset;
                """;

            parameters.Add(new NpgsqlParameter("limit", perPage));
            parameters.Add(new NpgsqlParameter("offset", (page - 1) * perPage));

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddRange(parameters.ToArray());

            var items = new List<FeaturedFixtureDto>();
            var total = 0;

            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                if (total == 0)
                    total = reader.GetInt32(reader.GetOrdinal("total_count"));

                items.Add(new FeaturedFixtureDto
                {
                    Id = reader.GetGuid(reader.GetOrdinal("id")),
                    FixtureId = reader.GetInt64(reader.GetOrdinal("fixture_id")),
                    FeatureDate = reader.GetDateTime(reader.GetOrdinal("feature_date")),
                    Source = reader.GetString(reader.GetOrdinal("source")),
                    Title = ReadNullableString(reader, "title"),
                    Description = ReadNullableString(reader, "description"),
                    Priority = reader.GetInt32(reader.GetOrdinal("priority")),
                    Active = reader.GetBoolean(reader.GetOrdinal("active")),
                    CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_at"))
                });
            }

            return (items, total);
        }

        public async Task<(IReadOnlyList<TipDto> Items, int Total)> GetPublicTipsAsync(
            string? resultStatus,
            long? fixtureId,
            int page,
            int perPage,
            CancellationToken ct = default)
        {
            var sb = new StringBuilder("where visibility = 'public'");
            var parameters = new List<NpgsqlParameter>();

            if (!string.IsNullOrWhiteSpace(resultStatus))
            {
                sb.Append(" and result_status = @result_status");
                parameters.Add(new NpgsqlParameter("result_status", resultStatus.Trim()));
            }

            if (fixtureId.HasValue)
            {
                sb.Append(" and fixture_id = @fixture_id");
                parameters.Add(new NpgsqlParameter("fixture_id", fixtureId.Value));
            }

            var sql = $"""
                select id, fixture_id, feed_type, bookmaker_id, market_id, outcome_key,
                       label, odd_value, total, handicap, result_status, note,
                       published_at, settled_at, created_at,
                       count(*) over() as total_count
                from app.tips
                {sb}
                order by published_at desc nulls last, created_at desc
                limit @limit offset @offset;
                """;

            parameters.Add(new NpgsqlParameter("limit", perPage));
            parameters.Add(new NpgsqlParameter("offset", (page - 1) * perPage));

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddRange(parameters.ToArray());

            var items = new List<TipDto>();
            var total = 0;

            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                if (total == 0)
                    total = reader.GetInt32(reader.GetOrdinal("total_count"));

                items.Add(new TipDto
                {
                    Id = reader.GetGuid(reader.GetOrdinal("id")),
                    FixtureId = reader.GetInt64(reader.GetOrdinal("fixture_id")),
                    FeedType = reader.GetString(reader.GetOrdinal("feed_type")),
                    BookmakerId = reader.GetInt64(reader.GetOrdinal("bookmaker_id")),
                    MarketId = reader.GetInt64(reader.GetOrdinal("market_id")),
                    OutcomeKey = reader.GetString(reader.GetOrdinal("outcome_key")),
                    Label = reader.GetString(reader.GetOrdinal("label")),
                    OddValue = ReadNullableDecimal(reader, "odd_value"),
                    Total = ReadNullableString(reader, "total"),
                    Handicap = ReadNullableString(reader, "handicap"),
                    ResultStatus = reader.GetString(reader.GetOrdinal("result_status")),
                    Note = ReadNullableString(reader, "note"),
                    PublishedAt = ReadNullableDateTimeOffset(reader, "published_at"),
                    SettledAt = ReadNullableDateTimeOffset(reader, "settled_at"),
                    CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_at"))
                });
            }

            return (items, total);
        }

        public async Task<(IReadOnlyList<CouponSummaryDto> Items, int Total)> GetPublicCouponsAsync(
            string? status,
            int page,
            int perPage,
            CancellationToken ct = default)
        {
            var sb = new StringBuilder("where visibility = 'public'");
            var parameters = new List<NpgsqlParameter>();

            if (!string.IsNullOrWhiteSpace(status))
            {
                sb.Append(" and status = @status");
                parameters.Add(new NpgsqlParameter("status", status.Trim()));
            }

            var sql = $"""
                select id, public_code, title, total_rate, status,
                       starts_at, ends_at, published_at, settled_at, created_at,
                       count(*) over() as total_count
                from app.coupons
                {sb}
                order by published_at desc nulls last, created_at desc
                limit @limit offset @offset;
                """;

            parameters.Add(new NpgsqlParameter("limit", perPage));
            parameters.Add(new NpgsqlParameter("offset", (page - 1) * perPage));

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddRange(parameters.ToArray());

            var items = new List<CouponSummaryDto>();
            var total = 0;

            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                if (total == 0)
                    total = reader.GetInt32(reader.GetOrdinal("total_count"));

                items.Add(MapCouponSummary(reader));
            }

            return (items, total);
        }

        public async Task<CouponDetailDto?> GetCouponByPublicCodeAsync(
            string publicCode,
            CancellationToken ct = default)
        {
            await using var connection = await OpenAsync(ct);

            CouponSummaryDto? summary = null;
            Guid couponId = Guid.Empty;

            await using (var command = new NpgsqlCommand(
                """
                select id, public_code, title, total_rate, status,
                       starts_at, ends_at, published_at, settled_at, created_at
                from app.coupons
                where public_code = @public_code and visibility = 'public'
                limit 1;
                """, connection))
            {
                command.Parameters.Add(new NpgsqlParameter("public_code", publicCode));
                await using var reader = await command.ExecuteReaderAsync(ct);
                if (await reader.ReadAsync(ct))
                {
                    summary = MapCouponSummary(reader);
                    couponId = summary.Id;
                }
            }

            if (summary == null)
                return null;

            var items = new List<CouponItemDto>();

            await using (var command = new NpgsqlCommand(
                """
                select id, fixture_id, feed_type, bookmaker_id, market_id,
                       outcome_key, label, odd_value, total, handicap,
                       result_status, sort_order
                from app.coupon_items
                where coupon_id = @coupon_id
                order by sort_order, created_at;
                """, connection))
            {
                command.Parameters.Add(new NpgsqlParameter("coupon_id", couponId));
                await using var reader = await command.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    items.Add(new CouponItemDto
                    {
                        Id = reader.GetGuid(reader.GetOrdinal("id")),
                        FixtureId = reader.GetInt64(reader.GetOrdinal("fixture_id")),
                        FeedType = reader.GetString(reader.GetOrdinal("feed_type")),
                        BookmakerId = reader.GetInt64(reader.GetOrdinal("bookmaker_id")),
                        MarketId = reader.GetInt64(reader.GetOrdinal("market_id")),
                        OutcomeKey = reader.GetString(reader.GetOrdinal("outcome_key")),
                        Label = reader.GetString(reader.GetOrdinal("label")),
                        OddValue = ReadNullableDecimal(reader, "odd_value"),
                        Total = ReadNullableString(reader, "total"),
                        Handicap = ReadNullableString(reader, "handicap"),
                        ResultStatus = reader.GetString(reader.GetOrdinal("result_status")),
                        SortOrder = reader.GetInt32(reader.GetOrdinal("sort_order"))
                    });
                }
            }

            return new CouponDetailDto
            {
                Coupon = summary,
                Items = items
            };
        }

        public async Task<Guid> SubmitContactMessageAsync(
            string name,
            string email,
            string? subject,
            string message,
            string? locale,
            string? ipAddress,
            string? userAgent,
            CancellationToken ct = default)
        {
            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(
                """
                insert into app.contact_messages (name, email, subject, message, locale, ip_address, user_agent)
                values (@name, @email, @subject, @message, @locale, @ip_address::inet, @user_agent)
                returning id;
                """, connection);

            command.Parameters.Add(new NpgsqlParameter("name", name.Trim()));
            command.Parameters.Add(new NpgsqlParameter("email", email.Trim()));
            command.Parameters.Add(new NpgsqlParameter("subject", (object?)subject?.Trim() ?? DBNull.Value));
            command.Parameters.Add(new NpgsqlParameter("message", message.Trim()));
            command.Parameters.Add(new NpgsqlParameter("locale", (object?)locale?.Trim() ?? DBNull.Value));
            command.Parameters.Add(new NpgsqlParameter("ip_address", (object?)ipAddress ?? DBNull.Value));
            command.Parameters.Add(new NpgsqlParameter("user_agent", (object?)userAgent ?? DBNull.Value));

            var result = await command.ExecuteScalarAsync(ct);
            return (Guid)result!;
        }

        private static CouponSummaryDto MapCouponSummary(NpgsqlDataReader reader) => new()
        {
            Id = reader.GetGuid(reader.GetOrdinal("id")),
            PublicCode = reader.GetString(reader.GetOrdinal("public_code")),
            Title = ReadNullableString(reader, "title"),
            TotalRate = ReadNullableDecimal(reader, "total_rate"),
            Status = reader.GetString(reader.GetOrdinal("status")),
            StartsAt = ReadNullableDateTimeOffset(reader, "starts_at"),
            EndsAt = ReadNullableDateTimeOffset(reader, "ends_at"),
            PublishedAt = ReadNullableDateTimeOffset(reader, "published_at"),
            SettledAt = ReadNullableDateTimeOffset(reader, "settled_at"),
            CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_at"))
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

        private static decimal? ReadNullableDecimal(NpgsqlDataReader r, string column)
        {
            var i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? null : r.GetDecimal(i);
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
