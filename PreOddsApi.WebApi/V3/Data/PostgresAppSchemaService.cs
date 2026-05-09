using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Npgsql;
using PreOddsApi.WebApi.V3.Contracts;
using PreOddsApi.WebApi.V3.Dtos;

namespace PreOddsApi.WebApi.V3.Data
{
    public sealed class PostgresAppSchemaService : IAppSchemaService
    {
        private readonly NpgsqlDataSource _dataSource;

        public PostgresAppSchemaService(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
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

        public async Task<TipOutcome> CreateTipAsync(
            Guid userId,
            CreateTipRequest request,
            CancellationToken ct = default)
        {
            var visibility = (request.Visibility ?? "public").Trim().ToLowerInvariant();
            if (visibility != "public" && visibility != "private" && visibility != "unlisted")
                return TipOutcome.Fail("VALIDATION", "visibility must be public, private or unlisted.");

            var feedType = (request.FeedType ?? "standard").Trim().ToLowerInvariant();
            if (feedType != "standard" && feedType != "premium")
                return TipOutcome.Fail("VALIDATION", "feed_type must be standard or premium.");

            if (request.FixtureId <= 0 || request.BookmakerId <= 0 || request.MarketId <= 0)
                return TipOutcome.Fail("VALIDATION", "fixture_id, bookmaker_id and market_id are required.");

            if (string.IsNullOrWhiteSpace(request.OutcomeKey) || string.IsNullOrWhiteSpace(request.Label))
                return TipOutcome.Fail("VALIDATION", "outcome_key and label are required.");

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(
                """
                insert into app.tips (
                    user_id, fixture_id, odds_current_id, feed_type,
                    bookmaker_id, market_id, outcome_key, label,
                    odd_value, total, handicap, result_status, visibility, note,
                    published_at)
                values (
                    @user_id, @fixture_id, @odds_current_id, @feed_type,
                    @bookmaker_id, @market_id, @outcome_key, @label,
                    @odd_value, @total, @handicap, 'pending', @visibility, @note,
                    now())
                returning id, fixture_id, feed_type, bookmaker_id, market_id, outcome_key,
                          label, odd_value, total, handicap, result_status, note,
                          published_at, settled_at, created_at;
                """, connection);

            command.Parameters.Add(new NpgsqlParameter("user_id", userId));
            command.Parameters.Add(new NpgsqlParameter("fixture_id", request.FixtureId));
            command.Parameters.Add(new NpgsqlParameter("odds_current_id",
                (object?)request.OddsCurrentId ?? DBNull.Value));
            command.Parameters.Add(new NpgsqlParameter("feed_type", feedType));
            command.Parameters.Add(new NpgsqlParameter("bookmaker_id", request.BookmakerId));
            command.Parameters.Add(new NpgsqlParameter("market_id", request.MarketId));
            command.Parameters.Add(new NpgsqlParameter("outcome_key", request.OutcomeKey.Trim()));
            command.Parameters.Add(new NpgsqlParameter("label", request.Label.Trim()));
            command.Parameters.Add(new NpgsqlParameter("odd_value",
                (object?)request.OddValue ?? DBNull.Value));
            command.Parameters.Add(new NpgsqlParameter("total",
                (object?)(string.IsNullOrWhiteSpace(request.Total) ? null : request.Total.Trim()) ?? DBNull.Value));
            command.Parameters.Add(new NpgsqlParameter("handicap",
                (object?)(string.IsNullOrWhiteSpace(request.Handicap) ? null : request.Handicap.Trim()) ?? DBNull.Value));
            command.Parameters.Add(new NpgsqlParameter("visibility", visibility));
            command.Parameters.Add(new NpgsqlParameter("note",
                (object?)(string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim()) ?? DBNull.Value));

            await using var reader = await command.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
                return TipOutcome.Fail("VALIDATION", "Failed to create tip.");

            return TipOutcome.Ok(new TipDto
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

        public async Task<bool> DeleteTipAsync(
            Guid userId, Guid tipId, CancellationToken ct = default)
        {
            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(
                "delete from app.tips where id = @id and user_id = @user_id;", connection);
            command.Parameters.Add(new NpgsqlParameter("id", tipId));
            command.Parameters.Add(new NpgsqlParameter("user_id", userId));
            return await command.ExecuteNonQueryAsync(ct) > 0;
        }

        public async Task<CouponOutcome> CreateCouponAsync(
            Guid userId, CreateCouponRequest request, CancellationToken ct = default)
        {
            var visibility = (request.Visibility ?? "public").Trim().ToLowerInvariant();
            if (visibility != "public" && visibility != "private" && visibility != "unlisted")
                return CouponOutcome.Fail("VALIDATION", "visibility must be public, private or unlisted.");

            if (request.Items == null || request.Items.Count == 0)
                return CouponOutcome.Fail("VALIDATION", "items must contain at least one selection.");

            decimal? totalRate = 1m;
            foreach (var item in request.Items)
            {
                if (item.FixtureId <= 0 || item.BookmakerId <= 0 || item.MarketId <= 0 ||
                    string.IsNullOrWhiteSpace(item.OutcomeKey) || string.IsNullOrWhiteSpace(item.Label))
                    return CouponOutcome.Fail("VALIDATION",
                        "Each item requires fixture_id, bookmaker_id, market_id, outcome_key and label.");

                if (item.OddValue.HasValue && item.OddValue.Value > 0)
                    totalRate *= item.OddValue.Value;
                else
                    totalRate = null;
            }

            await using var connection = await OpenAsync(ct);
            await using var transaction = await connection.BeginTransactionAsync(ct);

            try
            {
                Guid couponId;
                CouponSummaryDto couponSummary;

                await using (var insertCoupon = new NpgsqlCommand(
                    """
                    insert into app.coupons (user_id, title, total_rate, status, visibility, starts_at, ends_at, published_at)
                    values (@user_id, @title, @total_rate, 'published', @visibility, @starts_at, @ends_at, now())
                    returning id, public_code, title, total_rate, status, starts_at, ends_at, published_at, settled_at, created_at;
                    """, connection, transaction))
                {
                    insertCoupon.Parameters.Add(new NpgsqlParameter("user_id", userId));
                    insertCoupon.Parameters.Add(new NpgsqlParameter("title",
                        (object?)(string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim()) ?? DBNull.Value));
                    insertCoupon.Parameters.Add(new NpgsqlParameter("total_rate",
                        (object?)totalRate ?? DBNull.Value));
                    insertCoupon.Parameters.Add(new NpgsqlParameter("visibility", visibility));
                    insertCoupon.Parameters.Add(new NpgsqlParameter("starts_at",
                        (object?)request.StartsAt ?? DBNull.Value));
                    insertCoupon.Parameters.Add(new NpgsqlParameter("ends_at",
                        (object?)request.EndsAt ?? DBNull.Value));

                    await using var reader = await insertCoupon.ExecuteReaderAsync(ct);
                    if (!await reader.ReadAsync(ct))
                        throw new InvalidOperationException("Coupon insert returned no row.");
                    couponSummary = MapCouponSummary(reader);
                    couponId = couponSummary.Id;
                }

                var items = new List<CouponItemDto>();
                foreach (var item in request.Items)
                {
                    var feedType = (item.FeedType ?? "standard").Trim().ToLowerInvariant();
                    if (feedType != "standard" && feedType != "premium")
                    {
                        await transaction.RollbackAsync(ct);
                        return CouponOutcome.Fail("VALIDATION", "Item feed_type must be standard or premium.");
                    }

                    await using var insertItem = new NpgsqlCommand(
                        """
                        insert into app.coupon_items (
                            coupon_id, fixture_id, odds_current_id, feed_type,
                            bookmaker_id, market_id, outcome_key, label,
                            odd_value, total, handicap, sort_order)
                        values (
                            @coupon_id, @fixture_id, @odds_current_id, @feed_type,
                            @bookmaker_id, @market_id, @outcome_key, @label,
                            @odd_value, @total, @handicap, @sort_order)
                        returning id, fixture_id, feed_type, bookmaker_id, market_id,
                                  outcome_key, label, odd_value, total, handicap,
                                  result_status, sort_order;
                        """, connection, transaction);

                    insertItem.Parameters.Add(new NpgsqlParameter("coupon_id", couponId));
                    insertItem.Parameters.Add(new NpgsqlParameter("fixture_id", item.FixtureId));
                    insertItem.Parameters.Add(new NpgsqlParameter("odds_current_id",
                        (object?)item.OddsCurrentId ?? DBNull.Value));
                    insertItem.Parameters.Add(new NpgsqlParameter("feed_type", feedType));
                    insertItem.Parameters.Add(new NpgsqlParameter("bookmaker_id", item.BookmakerId));
                    insertItem.Parameters.Add(new NpgsqlParameter("market_id", item.MarketId));
                    insertItem.Parameters.Add(new NpgsqlParameter("outcome_key", item.OutcomeKey.Trim()));
                    insertItem.Parameters.Add(new NpgsqlParameter("label", item.Label.Trim()));
                    insertItem.Parameters.Add(new NpgsqlParameter("odd_value",
                        (object?)item.OddValue ?? DBNull.Value));
                    insertItem.Parameters.Add(new NpgsqlParameter("total",
                        (object?)(string.IsNullOrWhiteSpace(item.Total) ? null : item.Total.Trim()) ?? DBNull.Value));
                    insertItem.Parameters.Add(new NpgsqlParameter("handicap",
                        (object?)(string.IsNullOrWhiteSpace(item.Handicap) ? null : item.Handicap.Trim()) ?? DBNull.Value));
                    insertItem.Parameters.Add(new NpgsqlParameter("sort_order", item.SortOrder));

                    await using var reader = await insertItem.ExecuteReaderAsync(ct);
                    if (await reader.ReadAsync(ct))
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

                await transaction.CommitAsync(ct);

                return CouponOutcome.Ok(new CouponDetailDto
                {
                    Coupon = couponSummary,
                    Items = items
                });
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                await transaction.RollbackAsync(ct);
                return CouponOutcome.Fail("CONFLICT",
                    "Duplicate item selection within the coupon.");
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }

        public async Task<bool> DeleteCouponAsync(
            Guid userId, Guid couponId, CancellationToken ct = default)
        {
            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(
                "delete from app.coupons where id = @id and user_id = @user_id;", connection);
            command.Parameters.Add(new NpgsqlParameter("id", couponId));
            command.Parameters.Add(new NpgsqlParameter("user_id", userId));
            return await command.ExecuteNonQueryAsync(ct) > 0;
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

        private Task<NpgsqlConnection> OpenAsync(CancellationToken ct)
            => _dataSource.OpenConnectionAsync(ct).AsTask();

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
