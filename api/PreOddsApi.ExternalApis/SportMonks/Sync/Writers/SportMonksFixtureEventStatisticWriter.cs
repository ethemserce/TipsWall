using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;
using PreOddsApi.Entities.SportMonks.Football.Statistics.V3;
using PreOddsApi.Entities.SportMonks.Football.V3;
using SportMonksEvent = PreOddsApi.Entities.SportMonks.Football.V3.Event;

namespace PreOddsApi.ExternalApis.SportMonks.Sync.Writers
{
    public sealed class SportMonksFixtureEventStatisticWriter : ISportMonksFixtureEventStatisticWriter
    {
        private readonly string? _connectionString;
        private readonly ILogger<SportMonksFixtureEventStatisticWriter> _logger;

        public SportMonksFixtureEventStatisticWriter(
            IConfiguration configuration,
            ILogger<SportMonksFixtureEventStatisticWriter> logger)
        {
            _connectionString = configuration.GetConnectionString("PreOddsApiPostgresDb");
            _logger = logger;
        }

        public async Task UpsertEventsAndStatisticsAsync(
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
                var eventCount = 0;
                var statisticCount = 0;

                foreach (var fixture in fixtureList)
                {
                    foreach (var fixtureEvent in fixture.Events ?? Enumerable.Empty<SportMonksEvent>())
                    {
                        if (await UpsertEventAsync(
                                connection,
                                transaction,
                                fixtureEvent,
                                fixture.Id,
                                cancellationToken))
                        {
                            eventCount++;
                        }
                    }

                    foreach (var statistic in fixture.Statistics ?? Enumerable.Empty<Statistic>())
                    {
                        if (await UpsertStatisticAsync(
                                connection,
                                transaction,
                                statistic,
                                fixture.Id,
                                cancellationToken))
                        {
                            statisticCount++;
                        }
                    }
                }

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "Upserted {EventCount} fixture events and {StatisticCount} fixture statistics into football detail schema.",
                    eventCount,
                    statisticCount);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private static async Task<bool> UpsertEventAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            SportMonksEvent fixtureEvent,
            long fallbackFixtureId,
            CancellationToken cancellationToken)
        {
            if (fixtureEvent.Id == 0)
            {
                return false;
            }

            var fixtureId = fixtureEvent.FixtureId == 0 ? fallbackFixtureId : fixtureEvent.FixtureId;
            if (fixtureId == 0)
            {
                return false;
            }

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into football.fixture_events (
                    id,
                    fixture_id,
                    period_id,
                    participant_id,
                    type_id,
                    sub_type_id,
                    player_id,
                    related_player_id,
                    coach_id,
                    section,
                    player_name,
                    related_player_name,
                    result,
                    info,
                    addition,
                    minute,
                    extra_minute,
                    injured,
                    on_bench,
                    last_synced_at)
                values (
                    @id,
                    @fixture_id,
                    (select id from football.fixture_periods where id = @period_id),
                    (select id from football.teams where id = @participant_id),
                    (select id from catalog.types where id = @type_id),
                    (select id from catalog.types where id = @sub_type_id),
                    (select id from football.players where id = @player_id),
                    (select id from football.players where id = @related_player_id),
                    (select id from football.coaches where id = @coach_id),
                    @section,
                    @player_name,
                    @related_player_name,
                    @result,
                    @info,
                    @addition,
                    @minute,
                    @extra_minute,
                    @injured,
                    @on_bench,
                    now())
                on conflict (id) do update set
                    fixture_id = excluded.fixture_id,
                    period_id = excluded.period_id,
                    participant_id = excluded.participant_id,
                    type_id = excluded.type_id,
                    sub_type_id = excluded.sub_type_id,
                    player_id = excluded.player_id,
                    related_player_id = excluded.related_player_id,
                    coach_id = excluded.coach_id,
                    section = excluded.section,
                    player_name = excluded.player_name,
                    related_player_name = excluded.related_player_name,
                    result = excluded.result,
                    info = excluded.info,
                    addition = excluded.addition,
                    minute = excluded.minute,
                    extra_minute = excluded.extra_minute,
                    injured = excluded.injured,
                    on_bench = excluded.on_bench,
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("id", fixtureEvent.Id));
            command.Parameters.Add(Parameter("fixture_id", fixtureId));
            command.Parameters.Add(BigIntParameter("period_id", NullIfZero(fixtureEvent.PeriodId)));
            command.Parameters.Add(BigIntParameter("participant_id", NullIfZero(fixtureEvent.ParticipantId)));
            command.Parameters.Add(BigIntParameter("type_id", NullIfZero(fixtureEvent.TypeId)));
            command.Parameters.Add(BigIntParameter("sub_type_id", NullIfZero(fixtureEvent.subTypeId)));
            command.Parameters.Add(BigIntParameter("player_id", NullIfZero(fixtureEvent.PlayerId)));
            command.Parameters.Add(BigIntParameter("related_player_id", NullIfZero(fixtureEvent.RelatedPlayerId)));
            command.Parameters.Add(BigIntParameter("coach_id", NullIfZero(fixtureEvent.coachId)));
            command.Parameters.Add(TextParameter("section", NullIfWhiteSpace(fixtureEvent.Section)));
            command.Parameters.Add(TextParameter("player_name", NullIfWhiteSpace(fixtureEvent.PlayerName)));
            command.Parameters.Add(TextParameter("related_player_name", NullIfWhiteSpace(fixtureEvent.RelatedPlayerName)));
            command.Parameters.Add(TextParameter("result", NullIfWhiteSpace(fixtureEvent.Result)));
            command.Parameters.Add(TextParameter("info", NullIfWhiteSpace(fixtureEvent.Info)));
            command.Parameters.Add(TextParameter("addition", NullIfWhiteSpace(fixtureEvent.Addition)));
            command.Parameters.Add(IntegerParameter("minute", fixtureEvent.Minute));
            command.Parameters.Add(IntegerParameter("extra_minute", fixtureEvent.ExtraMinute));
            command.Parameters.Add(BooleanParameter("injured", fixtureEvent.Injured));
            command.Parameters.Add(BooleanParameter("on_bench", fixtureEvent.OnBench));

            await command.ExecuteNonQueryAsync(cancellationToken);
            return true;
        }

        private static async Task<bool> UpsertStatisticAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Statistic statistic,
            long fallbackFixtureId,
            CancellationToken cancellationToken)
        {
            if (statistic.Id == 0)
            {
                return false;
            }

            var fixtureId = statistic.FixtureId == 0 ? fallbackFixtureId : statistic.FixtureId;
            if (fixtureId == 0)
            {
                return false;
            }

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into football.fixture_statistics (
                    id,
                    fixture_id,
                    participant_id,
                    type_id,
                    value,
                    location,
                    raw_data,
                    last_synced_at)
                values (
                    @id,
                    @fixture_id,
                    (select id from football.teams where id = @participant_id),
                    (select id from catalog.types where id = @type_id),
                    @value,
                    @location,
                    @raw_data,
                    now())
                on conflict (id) do update set
                    fixture_id = excluded.fixture_id,
                    participant_id = excluded.participant_id,
                    type_id = excluded.type_id,
                    value = excluded.value,
                    location = excluded.location,
                    raw_data = excluded.raw_data,
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("id", statistic.Id));
            command.Parameters.Add(Parameter("fixture_id", fixtureId));
            command.Parameters.Add(BigIntParameter("participant_id", NullIfZero(statistic.TeamId)));
            command.Parameters.Add(BigIntParameter("type_id", NullIfZero(statistic.TypeId)));
            command.Parameters.Add(NumericParameter("value", GetStatisticValue(statistic)));
            command.Parameters.Add(TextParameter("location", NullIfWhiteSpace(statistic.Location)));
            command.Parameters.Add(JsonbParameter("raw_data", statistic.Data));

            await command.ExecuteNonQueryAsync(cancellationToken);
            return true;
        }

        private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                throw new InvalidOperationException(
                    "PostgreSQL connection string 'PreOddsApiPostgresDb' is required for fixture event/statistic sync.");
            }

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }

        private static decimal? GetStatisticValue(Statistic statistic)
        {
            return TryConvertDecimal(statistic.Data?.Value);
        }

        private static decimal? TryConvertDecimal(object? value)
        {
            switch (value)
            {
                case null:
                    return null;
                case decimal decimalValue:
                    return decimalValue;
                case int intValue:
                    return intValue;
                case long longValue:
                    return longValue;
                case float floatValue:
                    return Convert.ToDecimal(floatValue, CultureInfo.InvariantCulture);
                case double doubleValue:
                    return Convert.ToDecimal(doubleValue, CultureInfo.InvariantCulture);
            }

            var text = Convert.ToString(value, CultureInfo.InvariantCulture);
            return decimal.TryParse(
                text,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var result)
                ? result
                : null;
        }

        private static string? NullIfWhiteSpace(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static long? NullIfZero(long? value)
        {
            return value.GetValueOrDefault() == 0 ? null : value;
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

        private static NpgsqlParameter TextParameter(string name, string? value)
        {
            return new NpgsqlParameter(name, NpgsqlDbType.Text)
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

        private static NpgsqlParameter JsonbParameter(string name, object? value)
        {
            return new NpgsqlParameter(name, NpgsqlDbType.Jsonb)
            {
                Value = value == null ? DBNull.Value : JsonConvert.SerializeObject(value)
            };
        }

        private static NpgsqlParameter Parameter(string name, object? value)
        {
            return new NpgsqlParameter(name, value ?? DBNull.Value);
        }
    }
}
