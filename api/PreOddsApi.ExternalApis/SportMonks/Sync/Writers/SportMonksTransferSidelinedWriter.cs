using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using PreOddsApi.Entities.SportMonks.Core.V3;
using PreOddsApi.Entities.SportMonks.Football.V3;

namespace PreOddsApi.ExternalApis.SportMonks.Sync.Writers
{
    public sealed class SportMonksTransferSidelinedWriter : ISportMonksTransferSidelinedWriter
    {
        private readonly string? _connectionString;
        private readonly ILogger<SportMonksTransferSidelinedWriter> _logger;

        public SportMonksTransferSidelinedWriter(
            IConfiguration configuration,
            ILogger<SportMonksTransferSidelinedWriter> logger)
        {
            _connectionString = Environment.GetEnvironmentVariable("PREODDS_POSTGRES_CONNECTION")
                ?? configuration.GetConnectionString("PreOddsApiPostgresDb");
            _logger = logger;
        }

        public async Task UpsertTransfersAsync(
            IEnumerable<Transfer> transfers,
            CancellationToken cancellationToken = default)
        {
            var transferList = transfers
                .Where(transfer => transfer != null && transfer.Id > 0)
                .GroupBy(transfer => transfer.Id)
                .Select(group => group.Last())
                .ToList();

            if (transferList.Count == 0)
            {
                return;
            }

            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                var playerCount = 0;
                var teamCount = 0;
                var typeCount = 0;
                var sportCount = 0;

                foreach (var transfer in transferList)
                {
                    if (transfer.Sport != null && transfer.Sport.Id > 0)
                    {
                        await UpsertSportAsync(connection, transaction, transfer.Sport, cancellationToken);
                        sportCount++;
                    }

                    if (transfer.Type != null && transfer.Type.Id > 0)
                    {
                        await UpsertTypeAsync(connection, transaction, transfer.Type, cancellationToken);
                        typeCount++;
                    }

                    if (transfer.Position != null && transfer.Position.Id > 0)
                    {
                        await UpsertTypeAsync(connection, transaction, transfer.Position, cancellationToken);
                        typeCount++;
                    }

                    if (transfer.DetailedPosition != null && transfer.DetailedPosition.Id > 0)
                    {
                        await UpsertTypeAsync(connection, transaction, transfer.DetailedPosition, cancellationToken);
                        typeCount++;
                    }

                    if (transfer.Player != null && transfer.Player.Id > 0)
                    {
                        await UpsertPlayerAsync(connection, transaction, transfer.Player, cancellationToken);
                        playerCount++;
                    }

                    if (transfer.FromTeam != null && transfer.FromTeam.Id > 0)
                    {
                        await UpsertTeamAsync(connection, transaction, transfer.FromTeam, cancellationToken);
                        teamCount++;
                    }

                    if (transfer.ToTeam != null && transfer.ToTeam.Id > 0)
                    {
                        await UpsertTeamAsync(connection, transaction, transfer.ToTeam, cancellationToken);
                        teamCount++;
                    }

                    await UpsertTransferAsync(connection, transaction, transfer, cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "Upserted {TransferCount} transfers with {PlayerCount} nested players, {TeamCount} nested teams, {TypeCount} types, and {SportCount} sports.",
                    transferList.Count,
                    playerCount,
                    teamCount,
                    typeCount,
                    sportCount);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task UpsertFixtureSidelinedAsync(
            IEnumerable<Fixture> fixtures,
            CancellationToken cancellationToken = default)
        {
            var fixtureList = fixtures
                .Where(fixture => fixture != null && fixture.Id > 0 && fixture.Sidelineds != null)
                .ToList();

            if (fixtureList.Count == 0)
            {
                return;
            }

            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                var sidelinedPlayerCount = 0;
                var fixtureSidelinedCount = 0;
                var playerCount = 0;
                var teamCount = 0;
                var typeCount = 0;

                foreach (var fixture in fixtureList)
                {
                    foreach (var sidelined in fixture.Sidelineds.Where(item => item != null))
                    {
                        if (sidelined.Player != null && sidelined.Player.Id > 0)
                        {
                            await UpsertPlayerAsync(connection, transaction, sidelined.Player, cancellationToken);
                            playerCount++;
                        }

                        if (sidelined.Type != null && sidelined.Type.Id > 0)
                        {
                            await UpsertTypeAsync(connection, transaction, sidelined.Type, cancellationToken);
                            typeCount++;
                        }

                        if (sidelined.Participant != null && sidelined.Participant.Id > 0)
                        {
                            await UpsertTeamAsync(connection, transaction, sidelined.Participant, cancellationToken);
                            teamCount++;
                        }

                        if (sidelined.Sideline?.Player != null && sidelined.Sideline.Player.Id > 0)
                        {
                            await UpsertPlayerAsync(connection, transaction, sidelined.Sideline.Player, cancellationToken);
                            playerCount++;
                        }

                        if (sidelined.Sideline?.Type != null && sidelined.Sideline.Type.Id > 0)
                        {
                            await UpsertTypeAsync(connection, transaction, sidelined.Sideline.Type, cancellationToken);
                            typeCount++;
                        }

                        if (sidelined.Sideline?.Team != null && sidelined.Sideline.Team.Id > 0)
                        {
                            await UpsertTeamAsync(connection, transaction, sidelined.Sideline.Team, cancellationToken);
                            teamCount++;
                        }

                        if (await UpsertSidelinedPlayerAsync(connection, transaction, sidelined, cancellationToken))
                        {
                            sidelinedPlayerCount++;
                        }

                        if (await UpsertFixtureSidelinedAsync(connection, transaction, fixture, sidelined, cancellationToken))
                        {
                            fixtureSidelinedCount++;
                        }
                    }
                }

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "Upserted {SidelinedPlayerCount} sidelined player rows and {FixtureSidelinedCount} fixture-sidelined links with {PlayerCount} nested players, {TeamCount} teams, and {TypeCount} types.",
                    sidelinedPlayerCount,
                    fixtureSidelinedCount,
                    playerCount,
                    teamCount,
                    typeCount);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private static async Task UpsertTransferAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Transfer transfer,
            CancellationToken cancellationToken)
        {
            var sportId = transfer.SportId.GetValueOrDefault();
            if (sportId == 0 && transfer.Sport?.Id > 0)
            {
                sportId = transfer.Sport.Id;
            }

            var playerId = transfer.PlayerId.GetValueOrDefault();
            if (playerId == 0 && transfer.Player?.Id > 0)
            {
                playerId = transfer.Player.Id;
            }

            var typeId = transfer.TypeId.GetValueOrDefault();
            if (typeId == 0 && transfer.Type?.Id > 0)
            {
                typeId = transfer.Type.Id;
            }

            var fromTeamId = transfer.FromTeamId.GetValueOrDefault();
            if (fromTeamId == 0 && transfer.FromTeam?.Id > 0)
            {
                fromTeamId = transfer.FromTeam.Id;
            }

            var toTeamId = transfer.ToTeamId.GetValueOrDefault();
            if (toTeamId == 0 && transfer.ToTeam?.Id > 0)
            {
                toTeamId = transfer.ToTeam.Id;
            }

            var positionId = transfer.PositionId.GetValueOrDefault();
            if (positionId == 0 && transfer.Position?.Id > 0)
            {
                positionId = transfer.Position.Id;
            }

            var detailedPositionId = transfer.DetailedPositionId.GetValueOrDefault();
            if (detailedPositionId == 0 && transfer.DetailedPosition?.Id > 0)
            {
                detailedPositionId = transfer.DetailedPosition.Id;
            }

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into football.transfers (
                    id,
                    sport_id,
                    player_id,
                    type_id,
                    from_team_id,
                    to_team_id,
                    position_id,
                    detailed_position_id,
                    transfer_date,
                    career_ended,
                    completed,
                    amount,
                    last_synced_at)
                values (
                    @id,
                    (select id from catalog.sports where id = @sport_id),
                    (select id from football.players where id = @player_id),
                    (select id from catalog.types where id = @type_id),
                    (select id from football.teams where id = @from_team_id),
                    (select id from football.teams where id = @to_team_id),
                    (select id from catalog.types where id = @position_id),
                    (select id from catalog.types where id = @detailed_position_id),
                    @transfer_date,
                    @career_ended,
                    @completed,
                    @amount,
                    now())
                on conflict (id) do update set
                    sport_id = coalesce(excluded.sport_id, football.transfers.sport_id),
                    player_id = coalesce(excluded.player_id, football.transfers.player_id),
                    type_id = coalesce(excluded.type_id, football.transfers.type_id),
                    from_team_id = coalesce(excluded.from_team_id, football.transfers.from_team_id),
                    to_team_id = coalesce(excluded.to_team_id, football.transfers.to_team_id),
                    position_id = coalesce(excluded.position_id, football.transfers.position_id),
                    detailed_position_id = coalesce(excluded.detailed_position_id, football.transfers.detailed_position_id),
                    transfer_date = coalesce(excluded.transfer_date, football.transfers.transfer_date),
                    career_ended = excluded.career_ended,
                    completed = excluded.completed,
                    amount = coalesce(excluded.amount, football.transfers.amount),
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("id", transfer.Id));
            command.Parameters.Add(BigIntParameter("sport_id", NullIfZero(sportId)));
            command.Parameters.Add(BigIntParameter("player_id", NullIfZero(playerId)));
            command.Parameters.Add(BigIntParameter("type_id", NullIfZero(typeId)));
            command.Parameters.Add(BigIntParameter("from_team_id", NullIfZero(fromTeamId)));
            command.Parameters.Add(BigIntParameter("to_team_id", NullIfZero(toTeamId)));
            command.Parameters.Add(BigIntParameter("position_id", NullIfZero(positionId)));
            command.Parameters.Add(BigIntParameter("detailed_position_id", NullIfZero(detailedPositionId)));
            command.Parameters.Add(DateParameter("transfer_date", transfer.Date));
            command.Parameters.Add(BooleanParameter("career_ended", transfer.CareerEnded));
            command.Parameters.Add(BooleanParameter("completed", transfer.Completed));
            command.Parameters.Add(NumericParameter("amount", TryParseAmount(transfer.Amount)));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task<bool> UpsertSidelinedPlayerAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Sidelined sidelined,
            CancellationToken cancellationToken)
        {
            var sidelinedId = GetSidelinedPlayerId(sidelined);
            if (sidelinedId == null)
            {
                return false;
            }

            var playerId = ResolveId(sidelined.Sideline?.PlayerId, sidelined.PlayerId, sidelined.Sideline?.Player?.Id, sidelined.Player?.Id);
            var typeId = ResolveId(sidelined.Sideline?.TypeId, sidelined.TypeId, sidelined.Sideline?.Type?.Id, sidelined.Type?.Id);
            var teamId = ResolveId(sidelined.Sideline?.TeamId, sidelined.Sideline?.Team?.Id, sidelined.ParticipantId, sidelined.Participant?.Id);
            var seasonId = ResolveId(sidelined.Sideline?.SeasonId, sidelined.Sideline?.Season?.Id);

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into football.sidelined_players (
                    id,
                    player_id,
                    team_id,
                    season_id,
                    type_id,
                    category,
                    start_date,
                    end_date,
                    games_missed,
                    completed,
                    last_synced_at)
                values (
                    @id,
                    (select id from football.players where id = @player_id),
                    (select id from football.teams where id = @team_id),
                    (select id from competition.seasons where id = @season_id),
                    (select id from catalog.types where id = @type_id),
                    @category,
                    @start_date,
                    @end_date,
                    @games_missed,
                    @completed,
                    now())
                on conflict (id) do update set
                    player_id = coalesce(excluded.player_id, football.sidelined_players.player_id),
                    team_id = coalesce(excluded.team_id, football.sidelined_players.team_id),
                    season_id = coalesce(excluded.season_id, football.sidelined_players.season_id),
                    type_id = coalesce(excluded.type_id, football.sidelined_players.type_id),
                    category = coalesce(excluded.category, football.sidelined_players.category),
                    start_date = coalesce(excluded.start_date, football.sidelined_players.start_date),
                    end_date = coalesce(excluded.end_date, football.sidelined_players.end_date),
                    games_missed = coalesce(excluded.games_missed, football.sidelined_players.games_missed),
                    completed = excluded.completed,
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("id", sidelinedId.Value));
            command.Parameters.Add(BigIntParameter("player_id", NullIfZero(playerId)));
            command.Parameters.Add(BigIntParameter("team_id", NullIfZero(teamId)));
            command.Parameters.Add(BigIntParameter("season_id", NullIfZero(seasonId)));
            command.Parameters.Add(BigIntParameter("type_id", NullIfZero(typeId)));
            command.Parameters.Add(TextParameter("category", NullIfWhiteSpace(sidelined.Sideline?.Category)));
            command.Parameters.Add(DateParameter("start_date", TryParseDate(sidelined.Sideline?.StartDate)));
            command.Parameters.Add(DateParameter("end_date", TryParseDate(sidelined.Sideline?.EndDate)));
            command.Parameters.Add(IntegerParameter("games_missed", sidelined.Sideline?.GamesMissed));
            command.Parameters.Add(BooleanParameter("completed", sidelined.Sideline?.Completed ?? false));

            await command.ExecuteNonQueryAsync(cancellationToken);
            return true;
        }

        private static async Task<bool> UpsertFixtureSidelinedAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Fixture fixture,
            Sidelined sidelined,
            CancellationToken cancellationToken)
        {
            var fixtureId = ResolveId(sidelined.FixtureId, sidelined.Fixture?.Id, fixture.Id);
            var sidelinedPlayerId = GetSidelinedPlayerId(sidelined);

            if (fixtureId == 0 || sidelinedPlayerId == null)
            {
                return false;
            }

            var fixtureSidelinedId = sidelined.Id > 0
                ? sidelined.Id
                : GenerateSyntheticFixtureSidelinedId(fixtureId, sidelinedPlayerId.Value);
            var participantId = ResolveId(sidelined.ParticipantId, sidelined.Participant?.Id, sidelined.Sideline?.TeamId, sidelined.Sideline?.Team?.Id);

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into football.fixture_sidelined (
                    id,
                    fixture_id,
                    sidelined_id,
                    participant_id,
                    last_synced_at)
                select
                    @id,
                    @fixture_id,
                    @sidelined_id,
                    (select id from football.teams where id = @participant_id),
                    now()
                where exists (select 1 from football.fixtures where id = @fixture_id)
                  and exists (select 1 from football.sidelined_players where id = @sidelined_id)
                on conflict (id) do update set
                    fixture_id = excluded.fixture_id,
                    sidelined_id = excluded.sidelined_id,
                    participant_id = coalesce(excluded.participant_id, football.fixture_sidelined.participant_id),
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("id", fixtureSidelinedId));
            command.Parameters.Add(Parameter("fixture_id", fixtureId));
            command.Parameters.Add(Parameter("sidelined_id", sidelinedPlayerId.Value));
            command.Parameters.Add(BigIntParameter("participant_id", NullIfZero(participantId)));

            return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
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
                insert into catalog.sports (id, name, code, last_synced_at)
                values (@id, @name, @code, now())
                on conflict (id) do update set
                    name = excluded.name,
                    code = coalesce(excluded.code, catalog.sports.code),
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("id", sport.Id));
            command.Parameters.Add(Parameter("name", GetRequiredName(sport.Name, "sport", sport.Id)));
            command.Parameters.Add(TextParameter("code", NullIfWhiteSpace(sport.Code)));

            await command.ExecuteNonQueryAsync(cancellationToken);
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
                    code = coalesce(excluded.code, catalog.types.code),
                    developer_name = coalesce(excluded.developer_name, catalog.types.developer_name),
                    model_type = coalesce(excluded.model_type, catalog.types.model_type),
                    stat_group = coalesce(excluded.stat_group, catalog.types.stat_group),
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

        private static async Task UpsertPlayerAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Player player,
            CancellationToken cancellationToken)
        {
            var sportId = ResolveId(player.SportId, player.Sport?.Id);
            var countryId = ResolveId(player.CountryId, player.Country?.Id);
            var nationalityId = ResolveId(player.NationalityId, player.Nationality?.Id);
            var cityId = ResolveId(player.CityId, player.City?.Id);

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
            command.Parameters.Add(Parameter("name", GetRequiredPlayerName(player)));
            command.Parameters.Add(TextParameter("display_name", NullIfWhiteSpace(player.DisplayName)));
            command.Parameters.Add(TextParameter("gender", NullIfWhiteSpace(player.Gender)));
            command.Parameters.Add(TextParameter("image_path", NullIfWhiteSpace(player.ImagePath)));
            command.Parameters.Add(IntegerParameter("height", player.Height));
            command.Parameters.Add(IntegerParameter("weight", player.Weight));
            command.Parameters.Add(DateParameter("date_of_birth", player.DateOfBirth));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task UpsertTeamAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Team team,
            CancellationToken cancellationToken)
        {
            var sportId = ResolveId(team.SportId, team.Sport?.Id);
            var venueId = ResolveId(team.VenueId, team.Venue?.Id);

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
                    "PostgreSQL connection string 'PreOddsApiPostgresDb' is required for transfer/sidelined sync.");
            }

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }

        private static long? GetSidelinedPlayerId(Sidelined sidelined)
        {
            return ResolveId(sidelined.SidelineId, sidelined.Sideline?.Id);
        }

        private static long GenerateSyntheticFixtureSidelinedId(long fixtureId, long sidelinedId)
        {
            var value = ((fixtureId % 3_000_000) * 1_000_000) + (sidelinedId % 1_000_000);
            return -Math.Max(1, value);
        }

        private static long ResolveId(params long?[] values)
        {
            return values.FirstOrDefault(value => value.GetValueOrDefault() > 0).GetValueOrDefault();
        }

        private static string GetRequiredPlayerName(Player player)
        {
            return FirstNonEmpty(
                player.Name,
                player.DisplayName,
                player.CommonName,
                string.Join(" ", new[] { player.FirstName, player.LastName }
                    .Where(value => !string.IsNullOrWhiteSpace(value))))
                ?? $"player-{player.Id}";
        }

        private static string GetRequiredName(string? value, string entityName, long id)
        {
            return string.IsNullOrWhiteSpace(value)
                ? $"{entityName}-{id}"
                : value.Trim();
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

        private static decimal? NullIfZero(decimal? value)
        {
            return value.GetValueOrDefault() == 0 ? null : value;
        }

        private static decimal? TryParseAmount(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return decimal.TryParse(
                value,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var amount)
                ? NullIfZero(amount)
                : null;
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

        private static DateOnly? TryParseDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                ? date
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
