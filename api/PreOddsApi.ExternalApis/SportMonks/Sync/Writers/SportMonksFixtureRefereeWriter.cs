using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using PreOddsApi.Entities.SportMonks.Football.V3;

namespace PreOddsApi.ExternalApis.SportMonks.Sync.Writers
{
    public sealed class SportMonksFixtureRefereeWriter : ISportMonksFixtureRefereeWriter
    {
        private readonly string? _connectionString;
        private readonly ILogger<SportMonksFixtureRefereeWriter> _logger;

        public SportMonksFixtureRefereeWriter(
            IConfiguration configuration,
            ILogger<SportMonksFixtureRefereeWriter> logger)
        {
            _connectionString = configuration.GetConnectionString("PreOddsApiPostgresDb");
            _logger = logger;
        }

        public async Task UpsertFixtureRefereesAsync(
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
                var refereeCount = 0;
                var fixtureRefereeCount = 0;

                foreach (var fixture in fixtureList)
                {
                    foreach (var referee in fixture.Referees ?? Enumerable.Empty<Referee>())
                    {
                        if (referee == null || referee.Id == 0)
                        {
                            continue;
                        }

                        await UpsertRefereeAsync(connection, transaction, referee, cancellationToken);
                        refereeCount++;

                        await UpsertFixtureRefereeAsync(
                            connection,
                            transaction,
                            fixture.Id,
                            referee,
                            cancellationToken);
                        fixtureRefereeCount++;
                    }
                }

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "Upserted {RefereeCount} referees and {FixtureRefereeCount} fixture referee assignments into football core schema.",
                    refereeCount,
                    fixtureRefereeCount);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private static async Task UpsertRefereeAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Referee referee,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into football.referees (
                    id,
                    sport_id,
                    country_id,
                    nationality_id,
                    city_id,
                    common_name,
                    first_name,
                    last_name,
                    name,
                    display_name,
                    image_path,
                    height,
                    weight,
                    date_of_birth,
                    gender,
                    last_synced_at)
                values (
                    @id,
                    (select id from catalog.sports where id = @sport_id),
                    (select id from catalog.countries where id = @country_id),
                    (select id from catalog.countries where id = @nationality_id),
                    (select id from catalog.cities where id = @city_id),
                    @common_name,
                    @first_name,
                    @last_name,
                    @name,
                    @display_name,
                    @image_path,
                    @height,
                    @weight,
                    @date_of_birth,
                    @gender,
                    now())
                on conflict (id) do update set
                    sport_id = coalesce(excluded.sport_id, football.referees.sport_id),
                    country_id = coalesce(excluded.country_id, football.referees.country_id),
                    nationality_id = coalesce(excluded.nationality_id, football.referees.nationality_id),
                    city_id = coalesce(excluded.city_id, football.referees.city_id),
                    common_name = coalesce(excluded.common_name, football.referees.common_name),
                    first_name = coalesce(excluded.first_name, football.referees.first_name),
                    last_name = coalesce(excluded.last_name, football.referees.last_name),
                    name = excluded.name,
                    display_name = coalesce(excluded.display_name, football.referees.display_name),
                    image_path = coalesce(excluded.image_path, football.referees.image_path),
                    height = coalesce(excluded.height, football.referees.height),
                    weight = coalesce(excluded.weight, football.referees.weight),
                    date_of_birth = coalesce(excluded.date_of_birth, football.referees.date_of_birth),
                    gender = coalesce(excluded.gender, football.referees.gender),
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("id", referee.Id));
            command.Parameters.Add(BigIntParameter("sport_id", NullIfZero(referee.SportId)));
            command.Parameters.Add(BigIntParameter("country_id", NullIfZero(referee.CountryId)));
            command.Parameters.Add(BigIntParameter("nationality_id", NullIfZero(referee.NationalityId)));
            command.Parameters.Add(BigIntParameter("city_id", NullIfZero(referee.CityId)));
            command.Parameters.Add(TextParameter("common_name", NullIfWhiteSpace(referee.CommonName)));
            command.Parameters.Add(TextParameter("first_name", NullIfWhiteSpace(referee.FirstName)));
            command.Parameters.Add(TextParameter("last_name", NullIfWhiteSpace(referee.LastName)));
            command.Parameters.Add(Parameter("name", GetRequiredName(referee)));
            command.Parameters.Add(TextParameter("display_name", NullIfWhiteSpace(referee.DisplayName)));
            command.Parameters.Add(TextParameter("image_path", NullIfWhiteSpace(referee.ImagePath)));
            command.Parameters.Add(IntegerParameter("height", NullIfZero(referee.Height)));
            command.Parameters.Add(IntegerParameter("weight", NullIfZero(referee.Weight)));
            command.Parameters.Add(DateParameter("date_of_birth", referee.DateOfBirth));
            command.Parameters.Add(TextParameter("gender", NullIfWhiteSpace(referee.Gender)));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task UpsertFixtureRefereeAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            long fixtureId,
            Referee referee,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into football.fixture_referees (
                    fixture_id,
                    referee_id,
                    role,
                    last_synced_at)
                values (
                    @fixture_id,
                    @referee_id,
                    @role,
                    now())
                on conflict (fixture_id, referee_id) do update set
                    role = excluded.role,
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("fixture_id", fixtureId));
            command.Parameters.Add(Parameter("referee_id", referee.Id));
            command.Parameters.Add(TextParameter("role", NullIfWhiteSpace(referee.Role)));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                throw new InvalidOperationException(
                    "PostgreSQL connection string 'PreOddsApiPostgresDb' is required for fixture referee sync.");
            }

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }

        private static string GetRequiredName(Referee referee)
        {
            var names = new[]
            {
                referee.Name,
                referee.DisplayName,
                referee.CommonName,
                string.Join(" ", new[] { referee.FirstName, referee.LastName }
                    .Where(value => !string.IsNullOrWhiteSpace(value)))
            };

            return names
                .Select(NullIfWhiteSpace)
                .FirstOrDefault(value => value != null)
                ?? $"referee-{referee.Id}";
        }

        private static string? NullIfWhiteSpace(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static long? NullIfZero(long? value)
        {
            return value.GetValueOrDefault() == 0 ? null : value;
        }

        private static int? NullIfZero(int? value)
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

        private static NpgsqlParameter TextParameter(string name, string? value)
        {
            return new NpgsqlParameter(name, NpgsqlDbType.Text)
            {
                Value = value ?? (object)DBNull.Value
            };
        }

        private static NpgsqlParameter DateParameter(string name, DateOnly? value)
        {
            return new NpgsqlParameter(name, NpgsqlDbType.Date)
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
