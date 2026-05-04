using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using PreOddsApi.Entities.SportMonks.Football;
using PreOddsApi.Entities.SportMonks.Football.V3;

namespace PreOddsApi.ExternalApis.SportMonks.Sync.Writers
{
    public sealed class SportMonksFixtureTrendCommentaryWriter : ISportMonksFixtureTrendCommentaryWriter
    {
        private readonly string? _connectionString;
        private readonly ILogger<SportMonksFixtureTrendCommentaryWriter> _logger;

        public SportMonksFixtureTrendCommentaryWriter(
            IConfiguration configuration,
            ILogger<SportMonksFixtureTrendCommentaryWriter> logger)
        {
            _connectionString = Environment.GetEnvironmentVariable("PREODDS_POSTGRES_CONNECTION")
                ?? configuration.GetConnectionString("PreOddsApiPostgresDb");
            _logger = logger;
        }

        public async Task UpsertTrendsAndCommentariesAsync(
            IEnumerable<Fixture> fixtures,
            CancellationToken cancellationToken = default)
        {
            var fixtureList = fixtures
                .Where(fixture => fixture != null && fixture.Id > 0)
                .GroupBy(fixture => fixture.Id)
                .Select(group => group.Last())
                .ToList();

            if (fixtureList.Count == 0)
            {
                return;
            }

            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                var trendCount = 0;
                var commentaryCount = 0;

                foreach (var fixture in fixtureList)
                {
                    foreach (var trend in GetFixtureTrends(fixture))
                    {
                        if (await UpsertTrendAsync(
                                connection,
                                transaction,
                                fixture.Id,
                                trend,
                                cancellationToken))
                        {
                            trendCount++;
                        }
                    }

                    foreach (var commentary in fixture.Comments ?? Enumerable.Empty<Commentary>())
                    {
                        if (await UpsertCommentaryAsync(
                                connection,
                                transaction,
                                fixture.Id,
                                commentary,
                                cancellationToken))
                        {
                            commentaryCount++;
                        }
                    }
                }

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "Upserted {TrendCount} fixture trends and {CommentaryCount} fixture commentaries.",
                    trendCount,
                    commentaryCount);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private static IEnumerable<Trend> GetFixtureTrends(Fixture fixture)
        {
            foreach (var trend in fixture.Trends ?? Enumerable.Empty<Trend>())
            {
                if (trend != null)
                {
                    yield return trend;
                }
            }

            foreach (var trend in fixture.Pressure ?? Enumerable.Empty<Trend>())
            {
                if (trend != null)
                {
                    yield return trend;
                }
            }
        }

        private static async Task<bool> UpsertTrendAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            long fallbackFixtureId,
            Trend trend,
            CancellationToken cancellationToken)
        {
            var fixtureId = ResolveId(trend.FixtureId, trend.Fixture?.Id, fallbackFixtureId);
            if (fixtureId == 0)
            {
                return false;
            }

            var participantId = ResolveId(trend.ParticipantId, trend.Participant?.Id);
            var typeId = ResolveId(trend.TypeId, trend.Type?.Id);
            var periodId = ResolveId(trend.PeriodId, trend.Period?.Id);
            var value = GetTrendValue(trend);
            var trendId = trend.Id > 0
                ? trend.Id
                : GenerateSyntheticTrendId(fixtureId, participantId, typeId, periodId, trend.Minute, value);

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into football.fixture_trends (
                    id,
                    fixture_id,
                    participant_id,
                    type_id,
                    period_id,
                    value,
                    minute,
                    last_synced_at)
                select
                    @id,
                    fixtures.id,
                    (select id from football.teams where id = @participant_id),
                    (select id from catalog.types where id = @type_id),
                    (select id from football.fixture_periods where id = @period_id),
                    @value,
                    @minute,
                    now()
                from football.fixtures fixtures
                where fixtures.id = @fixture_id
                on conflict (id) do update set
                    fixture_id = excluded.fixture_id,
                    participant_id = coalesce(excluded.participant_id, football.fixture_trends.participant_id),
                    type_id = coalesce(excluded.type_id, football.fixture_trends.type_id),
                    period_id = coalesce(excluded.period_id, football.fixture_trends.period_id),
                    value = coalesce(excluded.value, football.fixture_trends.value),
                    minute = coalesce(excluded.minute, football.fixture_trends.minute),
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("id", trendId));
            command.Parameters.Add(Parameter("fixture_id", fixtureId));
            command.Parameters.Add(BigIntParameter("participant_id", NullIfZero(participantId)));
            command.Parameters.Add(BigIntParameter("type_id", NullIfZero(typeId)));
            command.Parameters.Add(BigIntParameter("period_id", NullIfZero(periodId)));
            command.Parameters.Add(NumericParameter("value", value));
            command.Parameters.Add(IntegerParameter("minute", NullIfZero(trend.Minute)));

            return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
        }

        private static async Task<bool> UpsertCommentaryAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            long fallbackFixtureId,
            Commentary commentary,
            CancellationToken cancellationToken)
        {
            var fixtureId = ResolveId(commentary.FixtureId, commentary.Fixture?.Id, fallbackFixtureId);
            var comment = NullIfWhiteSpace(commentary.Comment);

            if (fixtureId == 0 || comment == null)
            {
                return false;
            }

            var commentaryId = commentary.Id > 0
                ? commentary.Id
                : GenerateSyntheticCommentaryId(fixtureId, commentary.Order, commentary.Minute, commentary.ExtraMinute, comment);

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into football.fixture_commentaries (
                    id,
                    fixture_id,
                    comment,
                    minute,
                    extra_minute,
                    is_goal,
                    is_important,
                    sort_order,
                    last_synced_at)
                select
                    @id,
                    fixtures.id,
                    @comment,
                    @minute,
                    @extra_minute,
                    @is_goal,
                    @is_important,
                    @sort_order,
                    now()
                from football.fixtures fixtures
                where fixtures.id = @fixture_id
                on conflict (id) do update set
                    fixture_id = excluded.fixture_id,
                    comment = excluded.comment,
                    minute = coalesce(excluded.minute, football.fixture_commentaries.minute),
                    extra_minute = coalesce(excluded.extra_minute, football.fixture_commentaries.extra_minute),
                    is_goal = excluded.is_goal,
                    is_important = excluded.is_important,
                    sort_order = coalesce(excluded.sort_order, football.fixture_commentaries.sort_order),
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("id", commentaryId));
            command.Parameters.Add(Parameter("fixture_id", fixtureId));
            command.Parameters.Add(Parameter("comment", comment));
            command.Parameters.Add(IntegerParameter("minute", NullIfZero(commentary.Minute)));
            command.Parameters.Add(IntegerParameter("extra_minute", NullIfZero(commentary.ExtraMinute)));
            command.Parameters.Add(BooleanParameter("is_goal", commentary.IsGoal ?? false));
            command.Parameters.Add(BooleanParameter("is_important", commentary.IsImportant ?? false));
            command.Parameters.Add(IntegerParameter("sort_order", NullIfZero(commentary.Order)));

            return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
        }

        private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                throw new InvalidOperationException(
                    "PostgreSQL connection string 'PreOddsApiPostgresDb' is required for fixture trend/commentary sync.");
            }

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }

        private static decimal? GetTrendValue(Trend trend)
        {
            return trend.Value ?? trend.Pressure;
        }

        private static long GenerateSyntheticTrendId(
            long fixtureId,
            long participantId,
            long typeId,
            long periodId,
            int? minute,
            decimal? value)
        {
            var key = string.Join(
                ":",
                fixtureId.ToString(CultureInfo.InvariantCulture),
                participantId.ToString(CultureInfo.InvariantCulture),
                typeId.ToString(CultureInfo.InvariantCulture),
                periodId.ToString(CultureInfo.InvariantCulture),
                minute.GetValueOrDefault().ToString(CultureInfo.InvariantCulture),
                value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
            return -Math.Max(1, StableHash(key));
        }

        private static long GenerateSyntheticCommentaryId(
            long fixtureId,
            int? order,
            int? minute,
            int? extraMinute,
            string comment)
        {
            var key = string.Join(
                ":",
                fixtureId.ToString(CultureInfo.InvariantCulture),
                order.GetValueOrDefault().ToString(CultureInfo.InvariantCulture),
                minute.GetValueOrDefault().ToString(CultureInfo.InvariantCulture),
                extraMinute.GetValueOrDefault().ToString(CultureInfo.InvariantCulture),
                comment);
            return -Math.Max(1, StableHash(key));
        }

        private static long StableHash(string value)
        {
            const ulong offset = 14695981039346656037;
            const ulong prime = 1099511628211;

            var hash = offset;
            foreach (var character in value)
            {
                hash ^= character;
                hash *= prime;
            }

            return (long)(hash % long.MaxValue);
        }

        private static long ResolveId(params long?[] values)
        {
            return values.FirstOrDefault(value => value.GetValueOrDefault() > 0).GetValueOrDefault();
        }

        private static string? NullIfWhiteSpace(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static int? NullIfZero(int? value)
        {
            return value.GetValueOrDefault() == 0 ? null : value;
        }

        private static long? NullIfZero(long value)
        {
            return value == 0 ? null : value;
        }

        private static NpgsqlParameter BigIntParameter(string name, long? value)
        {
            return new NpgsqlParameter(name, NpgsqlDbType.Bigint)
            {
                Value = value ?? (object)DBNull.Value
            };
        }

        private static NpgsqlParameter IntegerParameter(string name, int? value)
        {
            return new NpgsqlParameter(name, NpgsqlDbType.Integer)
            {
                Value = value ?? (object)DBNull.Value
            };
        }

        private static NpgsqlParameter NumericParameter(string name, decimal? value)
        {
            return new NpgsqlParameter(name, NpgsqlDbType.Numeric)
            {
                Value = value ?? (object)DBNull.Value
            };
        }

        private static NpgsqlParameter BooleanParameter(string name, bool? value)
        {
            return new NpgsqlParameter(name, NpgsqlDbType.Boolean)
            {
                Value = value ?? (object)DBNull.Value
            };
        }

        private static NpgsqlParameter Parameter(string name, object? value)
        {
            return new NpgsqlParameter(name, value ?? DBNull.Value);
        }
    }
}
