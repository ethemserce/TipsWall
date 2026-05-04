using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using PreOddsApi.Entities.SportMonks.Football.V3;

namespace PreOddsApi.ExternalApis.SportMonks.Sync.Writers
{
    public sealed class SportMonksPlayerCoachSquadRivalWriter : ISportMonksPlayerCoachSquadRivalWriter
    {
        private readonly string? _connectionString;
        private readonly ILogger<SportMonksPlayerCoachSquadRivalWriter> _logger;

        public SportMonksPlayerCoachSquadRivalWriter(
            IConfiguration configuration,
            ILogger<SportMonksPlayerCoachSquadRivalWriter> logger)
        {
            _connectionString = Environment.GetEnvironmentVariable("PREODDS_POSTGRES_CONNECTION")
                ?? configuration.GetConnectionString("PreOddsApiPostgresDb");
            _logger = logger;
        }

        public async Task UpsertPlayersAsync(
            IEnumerable<Player> players,
            CancellationToken cancellationToken = default)
        {
            var playerList = players
                .Where(player => player != null && player.Id > 0)
                .GroupBy(player => player.Id)
                .Select(group => group.Last())
                .ToList();

            if (playerList.Count == 0)
            {
                return;
            }

            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                foreach (var player in playerList)
                {
                    await UpsertPlayerAsync(connection, transaction, player, cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "Upserted {PlayerCount} SportMonks players into football.players.",
                    playerList.Count);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task UpsertCoachesAsync(
            IEnumerable<Coach> coaches,
            CancellationToken cancellationToken = default)
        {
            var coachList = coaches
                .Where(coach => coach != null && coach.Id > 0)
                .GroupBy(coach => coach.Id)
                .Select(group => group.Last())
                .ToList();

            if (coachList.Count == 0)
            {
                return;
            }

            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                var nestedPlayerCount = 0;

                foreach (var coach in coachList)
                {
                    if (coach.Players != null && coach.Players.Id > 0)
                    {
                        await UpsertPlayerAsync(connection, transaction, coach.Players, cancellationToken);
                        nestedPlayerCount++;
                    }

                    await UpsertCoachAsync(connection, transaction, coach, cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "Upserted {CoachCount} coaches and {NestedPlayerCount} nested coach player records into football.coaches.",
                    coachList.Count,
                    nestedPlayerCount);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task UpsertTeamSquadsAsync(
            IEnumerable<TeamSquad> teamSquads,
            CancellationToken cancellationToken = default)
        {
            var squadList = teamSquads
                .Where(squad => squad != null && squad.Id > 0)
                .GroupBy(squad => squad.Id)
                .Select(group => group.Last())
                .ToList();

            if (squadList.Count == 0)
            {
                return;
            }

            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                var playerCount = 0;
                var teamCount = 0;
                var squadCount = 0;

                foreach (var squad in squadList)
                {
                    if (squad.Player != null && squad.Player.Id > 0)
                    {
                        await UpsertPlayerAsync(connection, transaction, squad.Player, cancellationToken);
                        playerCount++;
                    }

                    if (squad.Team != null && squad.Team.Id > 0)
                    {
                        await UpsertTeamAsync(connection, transaction, squad.Team, cancellationToken);
                        teamCount++;
                    }

                    if (await UpsertTeamSquadAsync(connection, transaction, squad, cancellationToken))
                    {
                        squadCount++;
                    }
                }

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "Upserted {SquadCount} team squad rows with {PlayerCount} nested players and {TeamCount} nested teams.",
                    squadCount,
                    playerCount,
                    teamCount);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task UpsertTeamRivalsAsync(
            IEnumerable<Rival> rivals,
            CancellationToken cancellationToken = default)
        {
            var rivalList = rivals
                .Where(rival => rival != null && rival.Id > 0)
                .GroupBy(rival => rival.Id)
                .Select(group => group.Last())
                .ToList();

            if (rivalList.Count == 0)
            {
                return;
            }

            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                var teamCount = 0;
                var rivalCount = 0;

                foreach (var rival in rivalList)
                {
                    if (rival.Team != null && rival.Team.Id > 0)
                    {
                        await UpsertTeamAsync(connection, transaction, rival.Team, cancellationToken);
                        teamCount++;
                    }

                    if (rival.RivalTeam != null && rival.RivalTeam.Id > 0)
                    {
                        await UpsertTeamAsync(connection, transaction, rival.RivalTeam, cancellationToken);
                        teamCount++;
                    }

                    if (await UpsertTeamRivalAsync(connection, transaction, rival, cancellationToken))
                    {
                        rivalCount++;
                    }
                }

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "Upserted {RivalCount} team rival rows with {TeamCount} nested teams.",
                    rivalCount,
                    teamCount);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private static async Task UpsertPlayerAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Player player,
            CancellationToken cancellationToken)
        {
            var sportId = player.SportId.GetValueOrDefault();
            if (sportId == 0 && player.Sport?.Id > 0)
            {
                sportId = player.Sport.Id;
            }

            var countryId = player.CountryId.GetValueOrDefault();
            if (countryId == 0 && player.Country?.Id > 0)
            {
                countryId = player.Country.Id;
            }

            var nationalityId = player.NationalityId.GetValueOrDefault();
            if (nationalityId == 0 && player.Nationality?.Id > 0)
            {
                nationalityId = player.Nationality.Id;
            }

            var cityId = player.CityId.GetValueOrDefault();
            if (cityId == 0 && player.City?.Id > 0)
            {
                cityId = player.City.Id;
            }

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into football.players (
                    id,
                    sport_id,
                    country_id,
                    nationality_id,
                    city_id,
                    position_id,
                    detailed_position_id,
                    type_id,
                    common_name,
                    first_name,
                    last_name,
                    name,
                    display_name,
                    gender,
                    image_path,
                    height,
                    weight,
                    date_of_birth,
                    last_synced_at)
                values (
                    @id,
                    (select id from catalog.sports where id = @sport_id),
                    (select id from catalog.countries where id = @country_id),
                    (select id from catalog.countries where id = @nationality_id),
                    (select id from catalog.cities where id = @city_id),
                    (select id from catalog.types where id = @position_id),
                    (select id from catalog.types where id = @detailed_position_id),
                    (select id from catalog.types where id = @type_id),
                    @common_name,
                    @first_name,
                    @last_name,
                    @name,
                    @display_name,
                    @gender,
                    @image_path,
                    @height,
                    @weight,
                    @date_of_birth,
                    now())
                on conflict (id) do update set
                    sport_id = coalesce(excluded.sport_id, football.players.sport_id),
                    country_id = coalesce(excluded.country_id, football.players.country_id),
                    nationality_id = coalesce(excluded.nationality_id, football.players.nationality_id),
                    city_id = coalesce(excluded.city_id, football.players.city_id),
                    position_id = coalesce(excluded.position_id, football.players.position_id),
                    detailed_position_id = coalesce(excluded.detailed_position_id, football.players.detailed_position_id),
                    type_id = coalesce(excluded.type_id, football.players.type_id),
                    common_name = coalesce(excluded.common_name, football.players.common_name),
                    first_name = coalesce(excluded.first_name, football.players.first_name),
                    last_name = coalesce(excluded.last_name, football.players.last_name),
                    name = excluded.name,
                    display_name = coalesce(excluded.display_name, football.players.display_name),
                    gender = coalesce(excluded.gender, football.players.gender),
                    image_path = coalesce(excluded.image_path, football.players.image_path),
                    height = coalesce(excluded.height, football.players.height),
                    weight = coalesce(excluded.weight, football.players.weight),
                    date_of_birth = coalesce(excluded.date_of_birth, football.players.date_of_birth),
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("id", player.Id));
            command.Parameters.Add(BigIntParameter("sport_id", NullIfZero(sportId)));
            command.Parameters.Add(BigIntParameter("country_id", NullIfZero(countryId)));
            command.Parameters.Add(BigIntParameter("nationality_id", NullIfZero(nationalityId)));
            command.Parameters.Add(BigIntParameter("city_id", NullIfZero(cityId)));
            command.Parameters.Add(BigIntParameter("position_id", NullIfZero(player.PositionId)));
            command.Parameters.Add(BigIntParameter("detailed_position_id", NullIfZero(player.DetailedPositionId)));
            command.Parameters.Add(BigIntParameter("type_id", NullIfZero(player.TypeId)));
            command.Parameters.Add(TextParameter("common_name", NullIfWhiteSpace(player.CommonName)));
            command.Parameters.Add(TextParameter("first_name", NullIfWhiteSpace(player.FirstName)));
            command.Parameters.Add(TextParameter("last_name", NullIfWhiteSpace(player.LastName)));
            command.Parameters.Add(Parameter("name", GetRequiredName(player)));
            command.Parameters.Add(TextParameter("display_name", NullIfWhiteSpace(player.DisplayName)));
            command.Parameters.Add(TextParameter("gender", NullIfWhiteSpace(player.Gender)));
            command.Parameters.Add(TextParameter("image_path", NullIfWhiteSpace(player.ImagePath)));
            command.Parameters.Add(IntegerParameter("height", player.Height));
            command.Parameters.Add(IntegerParameter("weight", player.Weight));
            command.Parameters.Add(DateParameter("date_of_birth", player.DateOfBirth));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task UpsertCoachAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Coach coach,
            CancellationToken cancellationToken)
        {
            var playerId = coach.PlayerId.GetValueOrDefault();
            if (playerId == 0 && coach.Players?.Id > 0)
            {
                playerId = coach.Players.Id;
            }

            var countryId = coach.CountryId.GetValueOrDefault();
            if (countryId == 0 && coach.Country?.Id > 0)
            {
                countryId = coach.Country.Id;
            }

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into football.coaches (
                    id,
                    player_id,
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
                    (select id from football.players where id = @player_id),
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
                    player_id = coalesce(excluded.player_id, football.coaches.player_id),
                    sport_id = coalesce(excluded.sport_id, football.coaches.sport_id),
                    country_id = coalesce(excluded.country_id, football.coaches.country_id),
                    nationality_id = coalesce(excluded.nationality_id, football.coaches.nationality_id),
                    city_id = coalesce(excluded.city_id, football.coaches.city_id),
                    common_name = coalesce(excluded.common_name, football.coaches.common_name),
                    first_name = coalesce(excluded.first_name, football.coaches.first_name),
                    last_name = coalesce(excluded.last_name, football.coaches.last_name),
                    name = excluded.name,
                    display_name = coalesce(excluded.display_name, football.coaches.display_name),
                    image_path = coalesce(excluded.image_path, football.coaches.image_path),
                    height = coalesce(excluded.height, football.coaches.height),
                    weight = coalesce(excluded.weight, football.coaches.weight),
                    date_of_birth = coalesce(excluded.date_of_birth, football.coaches.date_of_birth),
                    gender = coalesce(excluded.gender, football.coaches.gender),
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("id", coach.Id));
            command.Parameters.Add(BigIntParameter("player_id", NullIfZero(playerId)));
            command.Parameters.Add(BigIntParameter("sport_id", NullIfZero(coach.SportId)));
            command.Parameters.Add(BigIntParameter("country_id", NullIfZero(countryId)));
            command.Parameters.Add(BigIntParameter("nationality_id", NullIfZero(coach.NationalityId)));
            command.Parameters.Add(BigIntParameter("city_id", NullIfZero(coach.CityId)));
            command.Parameters.Add(TextParameter("common_name", NullIfWhiteSpace(coach.CommonName)));
            command.Parameters.Add(TextParameter("first_name", NullIfWhiteSpace(coach.FirstName)));
            command.Parameters.Add(TextParameter("last_name", NullIfWhiteSpace(coach.LastName)));
            command.Parameters.Add(Parameter("name", GetRequiredName(coach)));
            command.Parameters.Add(TextParameter("display_name", NullIfWhiteSpace(coach.DisplayName)));
            command.Parameters.Add(TextParameter("image_path", NullIfWhiteSpace(coach.ImagePath)));
            command.Parameters.Add(IntegerParameter("height", coach.Height));
            command.Parameters.Add(IntegerParameter("weight", coach.Weight));
            command.Parameters.Add(DateParameter("date_of_birth", coach.DateOfBirth));
            command.Parameters.Add(TextParameter("gender", NullIfWhiteSpace(coach.Gender)));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task<bool> UpsertTeamSquadAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            TeamSquad squad,
            CancellationToken cancellationToken)
        {
            var playerId = squad.PlayerId > 0 ? squad.PlayerId : squad.Player?.Id ?? 0;
            var teamId = squad.TeamId > 0 ? squad.TeamId : squad.Team?.Id ?? 0;

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into football.team_squads (
                    id,
                    season_id,
                    transfer_id,
                    player_id,
                    team_id,
                    position_id,
                    detailed_position_id,
                    jersey_number,
                    captain,
                    starts_at,
                    ends_at,
                    last_synced_at)
                select
                    @id,
                    (select id from competition.seasons where id = @season_id),
                    (select id from football.transfers where id = @transfer_id),
                    @player_id,
                    @team_id,
                    (select id from catalog.types where id = @position_id),
                    (select id from catalog.types where id = @detailed_position_id),
                    @jersey_number,
                    @captain,
                    @starts_at,
                    @ends_at,
                    now()
                where exists (select 1 from football.players where id = @player_id)
                  and exists (select 1 from football.teams where id = @team_id)
                on conflict (id) do update set
                    season_id = coalesce(excluded.season_id, football.team_squads.season_id),
                    transfer_id = coalesce(excluded.transfer_id, football.team_squads.transfer_id),
                    player_id = excluded.player_id,
                    team_id = excluded.team_id,
                    position_id = coalesce(excluded.position_id, football.team_squads.position_id),
                    detailed_position_id = coalesce(excluded.detailed_position_id, football.team_squads.detailed_position_id),
                    jersey_number = coalesce(excluded.jersey_number, football.team_squads.jersey_number),
                    captain = coalesce(excluded.captain, football.team_squads.captain),
                    starts_at = coalesce(excluded.starts_at, football.team_squads.starts_at),
                    ends_at = coalesce(excluded.ends_at, football.team_squads.ends_at),
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("id", squad.Id));
            command.Parameters.Add(BigIntParameter("season_id", NullIfZero(squad.SeasonId)));
            command.Parameters.Add(BigIntParameter("transfer_id", NullIfZero(squad.TransferId)));
            command.Parameters.Add(Parameter("player_id", playerId));
            command.Parameters.Add(Parameter("team_id", teamId));
            command.Parameters.Add(BigIntParameter("position_id", NullIfZero(squad.PositionId)));
            command.Parameters.Add(BigIntParameter("detailed_position_id", NullIfZero(squad.DetailedPositionId)));
            command.Parameters.Add(IntegerParameter("jersey_number", NullIfZero(squad.JerseyNumber)));
            command.Parameters.Add(BooleanParameter("captain", squad.Captain));
            command.Parameters.Add(DateParameter("starts_at", squad.Start));
            command.Parameters.Add(DateParameter("ends_at", squad.End));

            return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
        }

        private static async Task<bool> UpsertTeamRivalAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Rival rival,
            CancellationToken cancellationToken)
        {
            var teamId = rival.TeamId > 0 ? rival.TeamId : rival.Team?.Id ?? 0;
            var rivalTeamId = rival.RivalId > 0 ? rival.RivalId : rival.RivalTeam?.Id ?? 0;
            var sportId = rival.SportId;
            if (sportId == 0)
            {
                sportId = rival.Team?.SportId ?? rival.RivalTeam?.SportId ?? 0;
            }

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into football.team_rivals (
                    id,
                    sport_id,
                    team_id,
                    rival_team_id,
                    last_synced_at)
                select
                    @id,
                    (select id from catalog.sports where id = @sport_id),
                    @team_id,
                    @rival_team_id,
                    now()
                where exists (select 1 from football.teams where id = @team_id)
                  and exists (select 1 from football.teams where id = @rival_team_id)
                on conflict (id) do update set
                    sport_id = coalesce(excluded.sport_id, football.team_rivals.sport_id),
                    team_id = excluded.team_id,
                    rival_team_id = excluded.rival_team_id,
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("id", rival.Id));
            command.Parameters.Add(BigIntParameter("sport_id", NullIfZero(sportId)));
            command.Parameters.Add(Parameter("team_id", teamId));
            command.Parameters.Add(Parameter("rival_team_id", rivalTeamId));

            return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
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
            command.Parameters.Add(Parameter("id", team.Id));
            command.Parameters.Add(BigIntParameter("sport_id", NullIfZero(sportId)));
            command.Parameters.Add(BigIntParameter("country_id", NullIfZero(team.CountryId)));
            command.Parameters.Add(BigIntParameter("venue_id", NullIfZero(venueId)));
            command.Parameters.Add(TextParameter("gender", NullIfWhiteSpace(team.Gender)));
            command.Parameters.Add(Parameter("name", GetRequiredName(team)));
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
                    "PostgreSQL connection string 'PreOddsApiPostgresDb' is required for player/coach/squad/rival sync.");
            }

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }

        private static string GetRequiredName(Player player)
        {
            return FirstNonEmpty(
                player.Name,
                player.DisplayName,
                player.CommonName,
                string.Join(" ", new[] { player.FirstName, player.LastName }
                    .Where(value => !string.IsNullOrWhiteSpace(value))))
                ?? $"player-{player.Id}";
        }

        private static string GetRequiredName(Coach coach)
        {
            return FirstNonEmpty(
                coach.Name,
                coach.DisplayName,
                coach.CommonName,
                string.Join(" ", new[] { coach.FirstName, coach.LastName }
                    .Where(value => !string.IsNullOrWhiteSpace(value))))
                ?? $"coach-{coach.Id}";
        }

        private static string GetRequiredName(Team team)
        {
            return FirstNonEmpty(team.Name, team.ShortCode) ?? $"team-{team.Id}";
        }

        private static string? FirstNonEmpty(params string?[] values)
        {
            return values
                .Select(NullIfWhiteSpace)
                .FirstOrDefault(value => value != null);
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

        private static NpgsqlParameter DateParameter(string name, DateOnly? value)
        {
            return new NpgsqlParameter(name, NpgsqlDbType.Date)
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

        private static NpgsqlParameter Parameter(string name, object? value)
        {
            return new NpgsqlParameter(name, value ?? DBNull.Value);
        }
    }
}
