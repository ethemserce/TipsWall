using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using PreOddsApi.Entities.SportMonks.Core.V3;
using PreOddsApi.Entities.SportMonks.Football.V3;

namespace PreOddsApi.ExternalApis.SportMonks.Sync.Writers
{
    public sealed class SportMonksFootballCoreReferenceWriter : ISportMonksFootballCoreReferenceWriter
    {
        private readonly string? _connectionString;
        private readonly ILogger<SportMonksFootballCoreReferenceWriter> _logger;

        public SportMonksFootballCoreReferenceWriter(
            IConfiguration configuration,
            ILogger<SportMonksFootballCoreReferenceWriter> logger)
        {
            _connectionString = configuration.GetConnectionString("PreOddsApiPostgresDb");
            _logger = logger;
        }

        public async Task UpsertStatesAsync(
            IEnumerable<State> states,
            CancellationToken cancellationToken = default)
        {
            var stateList = states
                .Where(state => state != null && state.Id > 0)
                .GroupBy(state => state.Id)
                .Select(group => group.Last())
                .ToList();

            if (stateList.Count == 0)
            {
                return;
            }

            var typeList = stateList
                .Select(state => state.Type)
                .Where(type => type != null && type.Id > 0)
                .GroupBy(type => type.Id)
                .Select(group => group.Last())
                .ToList();

            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                foreach (var type in typeList)
                {
                    await UpsertTypeAsync(connection, transaction, type, cancellationToken);
                }

                foreach (var state in stateList)
                {
                    await UpsertStateAsync(connection, transaction, state, cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "Upserted {StateCount} states and {TypeCount} state types into catalog schema.",
                    stateList.Count,
                    typeList.Count);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task UpsertVenuesAsync(
            IEnumerable<Venue> venues,
            CancellationToken cancellationToken = default)
        {
            var venueList = venues
                .Where(venue => venue != null && venue.Id > 0)
                .GroupBy(venue => venue.Id)
                .Select(group => group.Last())
                .ToList();

            if (venueList.Count == 0)
            {
                return;
            }

            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                foreach (var venue in venueList)
                {
                    await UpsertVenueAsync(connection, transaction, venue, cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "Upserted {VenueCount} SportMonks venues into football.venues.",
                    venueList.Count);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task UpsertTeamsWithVenuesAsync(
            IEnumerable<Team> teams,
            CancellationToken cancellationToken = default)
        {
            var teamList = teams
                .Where(team => team != null && team.Id > 0)
                .GroupBy(team => team.Id)
                .Select(group => group.Last())
                .ToList();

            if (teamList.Count == 0)
            {
                return;
            }

            var sportList = ExtractSports(teamList)
                .Where(sport => sport.Id > 0)
                .GroupBy(sport => sport.Id)
                .Select(group => group.OrderBy(sport => IsPlaceholderSport(sport) ? 1 : 0).First())
                .ToList();
            var venueList = teamList
                .Select(team => team.Venue)
                .Where(venue => venue != null && venue.Id > 0)
                .GroupBy(venue => venue.Id)
                .Select(group => group.Last())
                .ToList();

            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                foreach (var sport in sportList)
                {
                    await UpsertSportAsync(connection, transaction, sport, cancellationToken);
                }

                foreach (var venue in venueList)
                {
                    await UpsertVenueAsync(connection, transaction, venue, cancellationToken);
                }

                foreach (var team in teamList)
                {
                    await UpsertTeamAsync(connection, transaction, team, cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "Upserted {SportCount} sports, {VenueCount} nested venues, and {TeamCount} teams into football core schema.",
                    sportList.Count,
                    venueList.Count,
                    teamList.Count);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private static async Task UpsertTypeAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Types type,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into catalog.types (
                    id,
                    name,
                    code,
                    developer_name,
                    model_type,
                    stat_group,
                    last_synced_at)
                values (
                    @id,
                    @name,
                    @code,
                    @developer_name,
                    @model_type,
                    @stat_group,
                    now())
                on conflict (id) do update set
                    name = excluded.name,
                    code = excluded.code,
                    developer_name = excluded.developer_name,
                    model_type = excluded.model_type,
                    stat_group = excluded.stat_group,
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("id", type.Id));
            command.Parameters.Add(Parameter("name", GetRequiredName(type.Name, "type", type.Id)));
            command.Parameters.Add(TextParameter("code", NullIfWhiteSpace(type.Code)));
            command.Parameters.Add(TextParameter("developer_name", NullIfWhiteSpace(type.DeveloperName)));
            command.Parameters.Add(TextParameter("model_type", NullIfWhiteSpace(type.ModelType)));
            command.Parameters.Add(TextParameter("stat_group", NullIfWhiteSpace(type.StatGroup)));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task UpsertStateAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            State state,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into catalog.states (
                    id,
                    type_id,
                    state_code,
                    name,
                    short_name,
                    developer_name,
                    last_synced_at)
                values (
                    @id,
                    (select id from catalog.types where id = @type_id),
                    @state_code,
                    @name,
                    @short_name,
                    @developer_name,
                    now())
                on conflict (id) do update set
                    type_id = excluded.type_id,
                    state_code = excluded.state_code,
                    name = excluded.name,
                    short_name = excluded.short_name,
                    developer_name = excluded.developer_name,
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("id", state.Id));
            command.Parameters.Add(BigIntParameter("type_id", NullIfZero(state.Type?.Id)));
            command.Parameters.Add(TextParameter("state_code", NullIfWhiteSpace(state.StateCode)));
            command.Parameters.Add(Parameter("name", GetRequiredName(state.Name, "state", state.Id)));
            command.Parameters.Add(TextParameter("short_name", NullIfWhiteSpace(state.ShortName)));
            command.Parameters.Add(TextParameter("developer_name", NullIfWhiteSpace(state.DeveloperName)));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task UpsertSportAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Sport sport,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into catalog.sports (
                    id,
                    name,
                    code,
                    last_synced_at)
                values (
                    @id,
                    @name,
                    @code,
                    now())
                on conflict (id) do update set
                    name = case
                        when @is_placeholder then catalog.sports.name
                        else excluded.name
                    end,
                    code = coalesce(excluded.code, catalog.sports.code),
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("id", sport.Id));
            command.Parameters.Add(Parameter("name", GetRequiredName(sport.Name, "sport", sport.Id)));
            command.Parameters.Add(TextParameter("code", NullIfWhiteSpace(sport.Code)));
            command.Parameters.Add(Parameter("is_placeholder", IsPlaceholderSport(sport)));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task UpsertVenueAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Venue venue,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into football.venues (
                    id,
                    country_id,
                    city_id,
                    name,
                    address,
                    zipcode,
                    latitude,
                    longitude,
                    capacity,
                    image_path,
                    city_name,
                    surface,
                    national_team,
                    last_synced_at)
                values (
                    @id,
                    (select id from catalog.countries where id = @country_id),
                    (select id from catalog.cities where id = @city_id),
                    @name,
                    @address,
                    @zipcode,
                    @latitude,
                    @longitude,
                    @capacity,
                    @image_path,
                    @city_name,
                    @surface,
                    @national_team,
                    now())
                on conflict (id) do update set
                    country_id = excluded.country_id,
                    city_id = excluded.city_id,
                    name = excluded.name,
                    address = excluded.address,
                    zipcode = excluded.zipcode,
                    latitude = excluded.latitude,
                    longitude = excluded.longitude,
                    capacity = excluded.capacity,
                    image_path = excluded.image_path,
                    city_name = excluded.city_name,
                    surface = excluded.surface,
                    national_team = excluded.national_team,
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("id", venue.Id));
            command.Parameters.Add(BigIntParameter("country_id", NullIfZero(venue.CountryId)));
            command.Parameters.Add(BigIntParameter("city_id", NullIfZero(venue.CityId)));
            command.Parameters.Add(Parameter("name", GetRequiredName(venue.Name, "venue", venue.Id)));
            command.Parameters.Add(TextParameter("address", NullIfWhiteSpace(venue.Address)));
            command.Parameters.Add(TextParameter("zipcode", NullIfWhiteSpace(venue.Zipcode)));
            command.Parameters.Add(NumericParameter("latitude", TryParseDecimal(venue.Latitude)));
            command.Parameters.Add(NumericParameter("longitude", TryParseDecimal(venue.Longitude)));
            command.Parameters.Add(IntegerParameter("capacity", NullIfZero(venue.Capacity)));
            command.Parameters.Add(TextParameter("image_path", NullIfWhiteSpace(venue.ImagePath)));
            command.Parameters.Add(TextParameter("city_name", NullIfWhiteSpace(venue.CityName)));
            command.Parameters.Add(TextParameter("surface", NullIfWhiteSpace(venue.Surface)));
            command.Parameters.Add(Parameter("national_team", venue.NationalTeam));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task UpsertTeamAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Team team,
            CancellationToken cancellationToken)
        {
            var sportId = team.SportId.GetValueOrDefault();
            if (sportId == 0 && team.Sport?.Id > 0)
            {
                sportId = team.Sport.Id;
            }

            var venueId = team.VenueId.GetValueOrDefault();
            if (venueId == 0 && team.Venue?.Id > 0)
            {
                venueId = team.Venue.Id;
            }

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into football.teams (
                    id,
                    sport_id,
                    country_id,
                    venue_id,
                    gender,
                    name,
                    short_code,
                    image_path,
                    founded,
                    type,
                    placeholder,
                    last_played_at,
                    last_synced_at)
                values (
                    @id,
                    (select id from catalog.sports where id = @sport_id),
                    (select id from catalog.countries where id = @country_id),
                    (select id from football.venues where id = @venue_id),
                    @gender,
                    @name,
                    @short_code,
                    @image_path,
                    @founded,
                    @type,
                    @placeholder,
                    @last_played_at,
                    now())
                on conflict (id) do update set
                    sport_id = excluded.sport_id,
                    country_id = excluded.country_id,
                    venue_id = excluded.venue_id,
                    gender = excluded.gender,
                    name = excluded.name,
                    short_code = excluded.short_code,
                    image_path = excluded.image_path,
                    founded = excluded.founded,
                    type = excluded.type,
                    placeholder = excluded.placeholder,
                    last_played_at = excluded.last_played_at,
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("id", team.Id));
            command.Parameters.Add(BigIntParameter("sport_id", NullIfZero(sportId)));
            command.Parameters.Add(BigIntParameter("country_id", NullIfZero(team.CountryId)));
            command.Parameters.Add(BigIntParameter("venue_id", NullIfZero(venueId)));
            command.Parameters.Add(TextParameter("gender", NullIfWhiteSpace(team.Gender)));
            command.Parameters.Add(Parameter("name", GetRequiredName(team.Name, "team", team.Id)));
            command.Parameters.Add(TextParameter("short_code", NullIfWhiteSpace(team.ShortCode)));
            command.Parameters.Add(TextParameter("image_path", NullIfWhiteSpace(team.ImagePath)));
            command.Parameters.Add(IntegerParameter("founded", ToInt32OrNull(team.Founded)));
            command.Parameters.Add(TextParameter("type", NullIfWhiteSpace(team.Type)));
            command.Parameters.Add(BooleanParameter("placeholder", team.Placeholder));
            command.Parameters.Add(TimestampTzParameter("last_played_at", TryParseUtcTimestamp(team.LastPlayedAt)));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                throw new InvalidOperationException(
                    "PostgreSQL connection string 'PreOddsApiPostgresDb' is required for football core reference sync.");
            }

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }

        private static IEnumerable<Sport> ExtractSports(IEnumerable<Team> teams)
        {
            foreach (var team in teams)
            {
                if (team.Sport != null)
                {
                    yield return team.Sport;
                }
                else
                {
                    var sportId = team.SportId.GetValueOrDefault();
                    if (sportId > 0)
                    {
                        yield return PlaceholderSport(sportId);
                    }
                }
            }
        }

        private static Sport PlaceholderSport(long id)
        {
            return new Sport
            {
                Id = id,
                Name = $"sport-{id}"
            };
        }

        private static bool IsPlaceholderSport(Sport sport)
        {
            return string.Equals(sport.Name, $"sport-{sport.Id}", StringComparison.Ordinal)
                && string.IsNullOrWhiteSpace(sport.Code);
        }

        private static string GetRequiredName(string? value, string entityName, long id)
        {
            return string.IsNullOrWhiteSpace(value)
                ? $"{entityName}-{id}"
                : value.Trim();
        }

        private static string? NullIfWhiteSpace(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static int? NullIfZero(int value)
        {
            return value == 0 ? null : value;
        }

        private static int? NullIfZero(int? value)
        {
            return value.GetValueOrDefault() == 0 ? null : value;
        }

        private static long? NullIfZero(long value)
        {
            return value == 0 ? null : value;
        }

        private static long? NullIfZero(long? value)
        {
            return value.GetValueOrDefault() == 0 ? null : value;
        }

        private static int? ToInt32OrNull(long? value)
        {
            if (!value.HasValue || value.Value == 0)
            {
                return null;
            }

            return value.Value is > int.MaxValue or < int.MinValue
                ? null
                : Convert.ToInt32(value.Value);
        }

        private static decimal? TryParseDecimal(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result)
                ? result
                : null;
        }

        private static DateTime? TryParseUtcTimestamp(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (DateTimeOffset.TryParse(
                    value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var dateTimeOffset))
            {
                return dateTimeOffset.UtcDateTime;
            }

            if (DateTime.TryParse(
                    value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var dateTime))
            {
                return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
            }

            return null;
        }

        private static DateTime? NormalizeUtc(DateTime? value)
        {
            if (!value.HasValue)
            {
                return null;
            }

            return value.Value.Kind switch
            {
                DateTimeKind.Utc => value.Value,
                DateTimeKind.Local => value.Value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
            };
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

        private static NpgsqlParameter TimestampTzParameter(string name, DateTime? value)
        {
            return new NpgsqlParameter(name, NpgsqlDbType.TimestampTz)
            {
                Value = NormalizeUtc(value) ?? (object)DBNull.Value
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

        private static NpgsqlParameter Parameter(string name, object? value)
        {
            return new NpgsqlParameter(name, value ?? DBNull.Value);
        }
    }
}
