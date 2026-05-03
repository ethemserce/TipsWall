using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;
using PreOddsApi.Entities.SportMonks.Core.Common.V3;
using PreOddsApi.Entities.SportMonks.Core.V3;
using PreOddsApi.Entities.SportMonks.Football.V3;

namespace PreOddsApi.ExternalApis.SportMonks.Sync.Writers
{
    public sealed class SportMonksFixtureCoreWriter : ISportMonksFixtureCoreWriter
    {
        private readonly string? _connectionString;
        private readonly ILogger<SportMonksFixtureCoreWriter> _logger;

        public SportMonksFixtureCoreWriter(
            IConfiguration configuration,
            ILogger<SportMonksFixtureCoreWriter> logger)
        {
            _connectionString = configuration.GetConnectionString("PreOddsApiPostgresDb");
            _logger = logger;
        }

        public async Task UpsertFixturesAsync(
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
                var participantCount = 0;
                var scoreCount = 0;
                var periodCount = 0;

                foreach (var fixture in fixtureList)
                {
                    var sport = ResolveSport(fixture);
                    if (sport != null)
                    {
                        await UpsertSportAsync(connection, transaction, sport, cancellationToken);
                    }

                    if (fixture.Venue?.Id > 0)
                    {
                        await UpsertVenueAsync(connection, transaction, fixture.Venue, cancellationToken);
                    }

                    foreach (var participant in fixture.Participants ?? Enumerable.Empty<Participant>())
                    {
                        await UpsertParticipantTeamAsync(connection, transaction, participant, cancellationToken);
                    }

                    await UpsertFixtureAsync(connection, transaction, fixture, cancellationToken);

                    foreach (var participant in fixture.Participants ?? Enumerable.Empty<Participant>())
                    {
                        await UpsertFixtureParticipantAsync(
                            connection,
                            transaction,
                            fixture.Id,
                            participant,
                            cancellationToken);
                        participantCount++;
                    }

                    foreach (var score in fixture.Scores ?? Enumerable.Empty<Score>())
                    {
                        await UpsertFixtureScoreAsync(
                            connection,
                            transaction,
                            score,
                            fixture.Id,
                            cancellationToken);
                        scoreCount++;
                    }

                    foreach (var period in fixture.Periods ?? Enumerable.Empty<Period>())
                    {
                        await UpsertFixturePeriodAsync(
                            connection,
                            transaction,
                            period,
                            fixture.Id,
                            cancellationToken);
                        periodCount++;
                    }
                }

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "Upserted {FixtureCount} fixtures, {ParticipantCount} participants, {ScoreCount} scores, and {PeriodCount} periods into football core schema.",
                    fixtureList.Count,
                    participantCount,
                    scoreCount,
                    periodCount);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
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

        private static async Task UpsertParticipantTeamAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Participant participant,
            CancellationToken cancellationToken)
        {
            if (participant.Id == 0)
            {
                return;
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
                    sport_id = coalesce(excluded.sport_id, football.teams.sport_id),
                    country_id = coalesce(excluded.country_id, football.teams.country_id),
                    venue_id = coalesce(excluded.venue_id, football.teams.venue_id),
                    gender = coalesce(excluded.gender, football.teams.gender),
                    name = excluded.name,
                    short_code = coalesce(excluded.short_code, football.teams.short_code),
                    image_path = coalesce(excluded.image_path, football.teams.image_path),
                    founded = coalesce(excluded.founded, football.teams.founded),
                    type = coalesce(excluded.type, football.teams.type),
                    placeholder = coalesce(excluded.placeholder, football.teams.placeholder),
                    last_played_at = coalesce(excluded.last_played_at, football.teams.last_played_at),
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("id", participant.Id));
            command.Parameters.Add(BigIntParameter("sport_id", NullIfZero(participant.SportId)));
            command.Parameters.Add(BigIntParameter("country_id", NullIfZero(participant.CountryId)));
            command.Parameters.Add(BigIntParameter("venue_id", NullIfZero(participant.VenueId)));
            command.Parameters.Add(TextParameter("gender", participant.Gender?.ToString().ToLowerInvariant()));
            command.Parameters.Add(Parameter("name", GetRequiredName(participant.Name, "team", participant.Id)));
            command.Parameters.Add(TextParameter("short_code", NullIfWhiteSpace(participant.ShortCode)));
            command.Parameters.Add(TextParameter("image_path", NullIfWhiteSpace(participant.ImagePath)));
            command.Parameters.Add(IntegerParameter("founded", ToInt32OrNull(participant.Founded)));
            command.Parameters.Add(TextParameter("type", NullIfWhiteSpace(participant.Type)));
            command.Parameters.Add(BooleanParameter("placeholder", participant.Placeholder));
            command.Parameters.Add(TimestampTzParameter("last_played_at", participant.LastPlayedAt));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task UpsertFixtureAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Fixture fixture,
            CancellationToken cancellationToken)
        {
            var sportId = ResolveRequiredId(fixture.SportId, fixture.Sport?.Id);
            var leagueId = ResolveRequiredId(fixture.LeagueId, fixture.League?.Id);
            var seasonId = ResolveOptionalId(fixture.SeasonId, fixture.Season?.Id);
            var stageId = ResolveOptionalId(fixture.StageId, fixture.Stage?.Id);
            var groupId = ResolveOptionalId(fixture.GroupId, fixture.Group?.Id);
            var aggregateId = ResolveOptionalId(fixture.AggregateId, fixture.Aggregate?.Id);
            var roundId = ResolveOptionalId(fixture.RoundId, fixture.Round?.Id);
            var stateId = ResolveOptionalId(fixture.StateId, fixture.State?.Id);
            var venueId = ResolveOptionalId(fixture.VenueId, fixture.Venue?.Id);

            EnsureRequiredForeignKey(sportId, "sport_id", "fixture", fixture.Id);
            EnsureRequiredForeignKey(leagueId, "league_id", "fixture", fixture.Id);

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into football.fixtures (
                    id,
                    sport_id,
                    league_id,
                    season_id,
                    stage_id,
                    group_id,
                    aggregate_id,
                    round_id,
                    state_id,
                    venue_id,
                    name,
                    result_info,
                    leg,
                    details,
                    length_minutes,
                    placeholder,
                    has_odds,
                    has_premium_odds,
                    starting_at,
                    starting_at_timestamp,
                    last_synced_at)
                values (
                    @id,
                    @sport_id,
                    @league_id,
                    (select id from competition.seasons where id = @season_id),
                    (select id from competition.stages where id = @stage_id),
                    (select id from competition.groups where id = @group_id),
                    (select id from competition.aggregates where id = @aggregate_id),
                    (select id from competition.rounds where id = @round_id),
                    (select id from catalog.states where id = @state_id),
                    (select id from football.venues where id = @venue_id),
                    @name,
                    @result_info,
                    @leg,
                    @details,
                    @length_minutes,
                    @placeholder,
                    @has_odds,
                    @has_premium_odds,
                    @starting_at,
                    @starting_at_timestamp,
                    now())
                on conflict (id) do update set
                    sport_id = excluded.sport_id,
                    league_id = excluded.league_id,
                    season_id = excluded.season_id,
                    stage_id = excluded.stage_id,
                    group_id = excluded.group_id,
                    aggregate_id = excluded.aggregate_id,
                    round_id = excluded.round_id,
                    state_id = excluded.state_id,
                    venue_id = excluded.venue_id,
                    name = excluded.name,
                    result_info = excluded.result_info,
                    leg = excluded.leg,
                    details = excluded.details,
                    length_minutes = excluded.length_minutes,
                    placeholder = excluded.placeholder,
                    has_odds = excluded.has_odds,
                    has_premium_odds = excluded.has_premium_odds,
                    starting_at = excluded.starting_at,
                    starting_at_timestamp = excluded.starting_at_timestamp,
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("id", fixture.Id));
            command.Parameters.Add(Parameter("sport_id", sportId));
            command.Parameters.Add(Parameter("league_id", leagueId));
            command.Parameters.Add(BigIntParameter("season_id", seasonId));
            command.Parameters.Add(BigIntParameter("stage_id", stageId));
            command.Parameters.Add(BigIntParameter("group_id", groupId));
            command.Parameters.Add(BigIntParameter("aggregate_id", aggregateId));
            command.Parameters.Add(BigIntParameter("round_id", roundId));
            command.Parameters.Add(BigIntParameter("state_id", stateId));
            command.Parameters.Add(BigIntParameter("venue_id", venueId));
            command.Parameters.Add(TextParameter("name", NullIfWhiteSpace(fixture.Name)));
            command.Parameters.Add(TextParameter("result_info", NullIfWhiteSpace(fixture.ResultInfo)));
            command.Parameters.Add(TextParameter("leg", NullIfWhiteSpace(fixture.Leg)));
            command.Parameters.Add(TextParameter("details", NullIfWhiteSpace(fixture.Details)));
            command.Parameters.Add(IntegerParameter("length_minutes", ToInt32OrNull(fixture.Length)));
            command.Parameters.Add(Parameter("placeholder", fixture.Placeholder));
            command.Parameters.Add(Parameter("has_odds", fixture.HasOdds));
            command.Parameters.Add(Parameter("has_premium_odds", fixture.HasPremiumOdds));
            command.Parameters.Add(TimestampTzParameter("starting_at", fixture.StartingAt));
            command.Parameters.Add(BigIntParameter("starting_at_timestamp", ToInt64OrNull(fixture.StartingAtTimestamp)));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task UpsertFixtureParticipantAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            long fixtureId,
            Participant participant,
            CancellationToken cancellationToken)
        {
            if (participant.Id == 0)
            {
                return;
            }

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into football.fixture_participants (
                    fixture_id,
                    team_id,
                    location,
                    winner,
                    position,
                    raw_meta,
                    last_synced_at)
                values (
                    @fixture_id,
                    @team_id,
                    @location,
                    @winner,
                    @position,
                    @raw_meta,
                    now())
                on conflict (fixture_id, team_id) do update set
                    location = excluded.location,
                    winner = excluded.winner,
                    position = excluded.position,
                    raw_meta = excluded.raw_meta,
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("fixture_id", fixtureId));
            command.Parameters.Add(Parameter("team_id", participant.Id));
            command.Parameters.Add(Parameter("location", GetParticipantLocation(participant.Meta)));
            command.Parameters.Add(BooleanParameter("winner", participant.Meta?.Winner));
            command.Parameters.Add(IntegerParameter("position", participant.Meta?.Position));
            command.Parameters.Add(JsonbParameter("raw_meta", participant.Meta));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task UpsertFixtureScoreAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Score score,
            long fallbackFixtureId,
            CancellationToken cancellationToken)
        {
            if (score.Id == 0)
            {
                return;
            }

            var fixtureId = score.FixtureId == 0 ? fallbackFixtureId : score.FixtureId;

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into football.fixture_scores (
                    id,
                    fixture_id,
                    type_id,
                    participant_id,
                    description,
                    goals,
                    participant_location,
                    raw_score,
                    last_synced_at)
                values (
                    @id,
                    @fixture_id,
                    (select id from catalog.types where id = @type_id),
                    (select id from football.teams where id = @participant_id),
                    @description,
                    @goals,
                    @participant_location,
                    @raw_score,
                    now())
                on conflict (id) do update set
                    fixture_id = excluded.fixture_id,
                    type_id = excluded.type_id,
                    participant_id = excluded.participant_id,
                    description = excluded.description,
                    goals = excluded.goals,
                    participant_location = excluded.participant_location,
                    raw_score = excluded.raw_score,
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("id", score.Id));
            command.Parameters.Add(Parameter("fixture_id", fixtureId));
            command.Parameters.Add(BigIntParameter("type_id", NullIfZero(score.TypeId)));
            command.Parameters.Add(BigIntParameter("participant_id", NullIfZero(score.ParticipantId)));
            command.Parameters.Add(TextParameter("description", NullIfWhiteSpace(score.Description)));
            command.Parameters.Add(IntegerParameter("goals", score.Goal?.Goals));
            command.Parameters.Add(TextParameter("participant_location", NullIfWhiteSpace(score.Goal?.Participant)));
            command.Parameters.Add(JsonbParameter("raw_score", score.Goal));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task UpsertFixturePeriodAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Period period,
            long fallbackFixtureId,
            CancellationToken cancellationToken)
        {
            if (period.Id == 0)
            {
                return;
            }

            var fixtureId = period.FixtureId == 0 ? fallbackFixtureId : period.FixtureId;

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into football.fixture_periods (
                    id,
                    fixture_id,
                    type_id,
                    started_at,
                    ended_at,
                    started_timestamp,
                    ended_timestamp,
                    counts_from,
                    actual_period_start,
                    ticking,
                    sort_order,
                    description,
                    time_added,
                    minutes,
                    seconds,
                    last_synced_at)
                values (
                    @id,
                    @fixture_id,
                    (select id from catalog.types where id = @type_id),
                    @started_at,
                    @ended_at,
                    @started_timestamp,
                    @ended_timestamp,
                    @counts_from,
                    @actual_period_start,
                    @ticking,
                    @sort_order,
                    @description,
                    @time_added,
                    @minutes,
                    @seconds,
                    now())
                on conflict (id) do update set
                    fixture_id = excluded.fixture_id,
                    type_id = excluded.type_id,
                    started_at = excluded.started_at,
                    ended_at = excluded.ended_at,
                    started_timestamp = excluded.started_timestamp,
                    ended_timestamp = excluded.ended_timestamp,
                    counts_from = excluded.counts_from,
                    actual_period_start = excluded.actual_period_start,
                    ticking = excluded.ticking,
                    sort_order = excluded.sort_order,
                    description = excluded.description,
                    time_added = excluded.time_added,
                    minutes = excluded.minutes,
                    seconds = excluded.seconds,
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("id", period.Id));
            command.Parameters.Add(Parameter("fixture_id", fixtureId));
            command.Parameters.Add(BigIntParameter("type_id", NullIfZero(period.TypeId)));
            command.Parameters.Add(TimestampTzParameter("started_at", UnixSecondsToUtc(period.Started)));
            command.Parameters.Add(TimestampTzParameter("ended_at", UnixSecondsToUtc(period.Ended)));
            command.Parameters.Add(BigIntParameter("started_timestamp", NullIfZero(period.Started)));
            command.Parameters.Add(BigIntParameter("ended_timestamp", NullIfZero(period.Ended)));
            command.Parameters.Add(Parameter("counts_from", period.CountsFrom));
            command.Parameters.Add(Parameter("actual_period_start", period.ActualPeriodStart));
            command.Parameters.Add(Parameter("ticking", period.Ticking));
            command.Parameters.Add(Parameter("sort_order", period.SortOrder));
            command.Parameters.Add(TextParameter("description", NullIfWhiteSpace(period.Description)));
            command.Parameters.Add(IntegerParameter("time_added", period.TimeAdded));
            command.Parameters.Add(Parameter("minutes", period.Minutes));
            command.Parameters.Add(Parameter("seconds", period.Seconds));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                throw new InvalidOperationException(
                    "PostgreSQL connection string 'PreOddsApiPostgresDb' is required for fixture core sync.");
            }

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }

        private static void EnsureRequiredForeignKey(
            long value,
            string columnName,
            string entityName,
            long entityId)
        {
            if (value == 0)
            {
                throw new InvalidOperationException(
                    $"SportMonks {entityName} {entityId} cannot be written without a {columnName}.");
            }
        }

        private static string GetParticipantLocation(Meta? meta)
        {
            return meta?.Location?.ToString().ToLowerInvariant() ?? "unknown";
        }

        private static Sport? ResolveSport(Fixture fixture)
        {
            if (fixture.Sport?.Id > 0)
            {
                return fixture.Sport;
            }

            return fixture.SportId > 0 ? PlaceholderSport(fixture.SportId) : null;
        }

        private static long ResolveRequiredId(long value, long? includedId)
        {
            return value != 0 ? value : includedId.GetValueOrDefault();
        }

        private static long? ResolveOptionalId(long value, long? includedId)
        {
            return NullIfZero(value) ?? NullIfZero(includedId);
        }

        private static long? ResolveOptionalId(long? value, long? includedId)
        {
            return NullIfZero(value) ?? NullIfZero(includedId);
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

        private static long? NullIfZero(long value)
        {
            return value == 0 ? null : value;
        }

        private static int? NullIfZero(int value)
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

        private static long? ToInt64OrNull(double value)
        {
            return value <= 0 ? null : Convert.ToInt64(value);
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

        private static DateTime? UnixSecondsToUtc(long value)
        {
            return value <= 0 ? null : DateTimeOffset.FromUnixTimeSeconds(value).UtcDateTime;
        }

        private static DateTime? UnixSecondsToUtc(long? value)
        {
            return value.GetValueOrDefault() <= 0
                ? null
                : DateTimeOffset.FromUnixTimeSeconds(value!.Value).UtcDateTime;
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

        private static NpgsqlParameter NumericParameter(string name, decimal? value)
        {
            return new NpgsqlParameter(name, NpgsqlDbType.Numeric)
            {
                Value = value ?? (object)DBNull.Value
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

        private static NpgsqlParameter TimestampTzParameter(string name, DateTime? value)
        {
            return new NpgsqlParameter(name, NpgsqlDbType.TimestampTz)
            {
                Value = NormalizeUtc(value) ?? (object)DBNull.Value
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
