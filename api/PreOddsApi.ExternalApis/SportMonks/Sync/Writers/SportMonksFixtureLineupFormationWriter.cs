using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;
using PreOddsApi.Entities.SportMonks.Football.V3;

namespace PreOddsApi.ExternalApis.SportMonks.Sync.Writers
{
    public sealed class SportMonksFixtureLineupFormationWriter : ISportMonksFixtureLineupFormationWriter
    {
        private readonly string? _connectionString;
        private readonly ILogger<SportMonksFixtureLineupFormationWriter> _logger;

        public SportMonksFixtureLineupFormationWriter(
            IConfiguration configuration,
            ILogger<SportMonksFixtureLineupFormationWriter> logger)
        {
            _connectionString = configuration.GetConnectionString("PreOddsApiPostgresDb");
            _logger = logger;
        }

        public async Task UpsertLineupsAndFormationsAsync(
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
                var lineupCount = 0;
                var lineupDetailCount = 0;
                var formationCount = 0;

                foreach (var fixture in fixtureList)
                {
                    foreach (var lineup in fixture.Lineups ?? Enumerable.Empty<Lineup>())
                    {
                        if (await UpsertLineupAsync(
                                connection,
                                transaction,
                                lineup,
                                fixture.Id,
                                cancellationToken))
                        {
                            lineupCount++;
                        }

                        foreach (var detail in lineup.Details ?? Enumerable.Empty<LineupDetail>())
                        {
                            if (await UpsertLineupDetailAsync(
                                    connection,
                                    transaction,
                                    detail,
                                    fixture.Id,
                                    lineup.Id,
                                    lineup.PlayerId,
                                    lineup.TeamId,
                                    cancellationToken))
                            {
                                lineupDetailCount++;
                            }
                        }
                    }

                    foreach (var formation in fixture.Formations ?? Enumerable.Empty<Formation>())
                    {
                        if (await UpsertFormationAsync(
                                connection,
                                transaction,
                                formation,
                                fixture.Id,
                                cancellationToken))
                        {
                            formationCount++;
                        }
                    }
                }

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "Upserted {LineupCount} fixture lineups, {LineupDetailCount} lineup details, and {FormationCount} formations into football detail schema.",
                    lineupCount,
                    lineupDetailCount,
                    formationCount);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private static async Task<bool> UpsertLineupAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Lineup lineup,
            long fallbackFixtureId,
            CancellationToken cancellationToken)
        {
            if (lineup.Id == 0)
            {
                return false;
            }

            var fixtureId = lineup.FixtureId.GetValueOrDefault() == 0
                ? fallbackFixtureId
                : lineup.FixtureId.GetValueOrDefault();
            if (fixtureId == 0)
            {
                return false;
            }

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into football.fixture_lineups (
                    id,
                    sport_id,
                    fixture_id,
                    player_id,
                    team_id,
                    position_id,
                    type_id,
                    formation_field,
                    jersey_number,
                    formation_position,
                    player_name,
                    last_synced_at)
                values (
                    @id,
                    (select id from catalog.sports where id = @sport_id),
                    @fixture_id,
                    (select id from football.players where id = @player_id),
                    (select id from football.teams where id = @team_id),
                    (select id from catalog.types where id = @position_id),
                    (select id from catalog.types where id = @type_id),
                    @formation_field,
                    @jersey_number,
                    @formation_position,
                    @player_name,
                    now())
                on conflict (id) do update set
                    sport_id = excluded.sport_id,
                    fixture_id = excluded.fixture_id,
                    player_id = excluded.player_id,
                    team_id = excluded.team_id,
                    position_id = excluded.position_id,
                    type_id = excluded.type_id,
                    formation_field = excluded.formation_field,
                    jersey_number = excluded.jersey_number,
                    formation_position = excluded.formation_position,
                    player_name = excluded.player_name,
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("id", lineup.Id));
            command.Parameters.Add(BigIntParameter("sport_id", NullIfZero(lineup.SportId)));
            command.Parameters.Add(Parameter("fixture_id", fixtureId));
            command.Parameters.Add(BigIntParameter("player_id", NullIfZero(lineup.PlayerId)));
            command.Parameters.Add(BigIntParameter("team_id", NullIfZero(lineup.TeamId)));
            command.Parameters.Add(BigIntParameter("position_id", NullIfZero(lineup.PositionId)));
            command.Parameters.Add(BigIntParameter("type_id", NullIfZero(lineup.TypeId)));
            command.Parameters.Add(TextParameter("formation_field", NullIfWhiteSpace(lineup.FormationField)));
            command.Parameters.Add(IntegerParameter("jersey_number", lineup.JerseyNumber));
            command.Parameters.Add(IntegerParameter("formation_position", lineup.FormationPosition));
            command.Parameters.Add(TextParameter("player_name", NullIfWhiteSpace(lineup.PlayerName)));

            await command.ExecuteNonQueryAsync(cancellationToken);
            return true;
        }

        private static async Task<bool> UpsertLineupDetailAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            LineupDetail detail,
            long fallbackFixtureId,
            long fallbackLineupId,
            long? fallbackPlayerId,
            long? fallbackTeamId,
            CancellationToken cancellationToken)
        {
            if (detail.Id == 0)
            {
                return false;
            }

            var fixtureId = detail.FixtureId == 0 ? fallbackFixtureId : detail.FixtureId;
            if (fixtureId == 0)
            {
                return false;
            }

            var lineupId = detail.LineupId == 0 ? fallbackLineupId : detail.LineupId;
            var playerId = detail.PlayerId == 0 ? fallbackPlayerId : detail.PlayerId;
            var teamId = detail.TeamId == 0 ? fallbackTeamId : detail.TeamId;

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into football.fixture_lineup_details (
                    id,
                    fixture_id,
                    lineup_id,
                    player_id,
                    team_id,
                    type_id,
                    data,
                    last_synced_at)
                values (
                    @id,
                    @fixture_id,
                    (select id from football.fixture_lineups where id = @lineup_id),
                    (select id from football.players where id = @player_id),
                    (select id from football.teams where id = @team_id),
                    (select id from catalog.types where id = @type_id),
                    @data,
                    now())
                on conflict (id) do update set
                    fixture_id = excluded.fixture_id,
                    lineup_id = excluded.lineup_id,
                    player_id = excluded.player_id,
                    team_id = excluded.team_id,
                    type_id = excluded.type_id,
                    data = excluded.data,
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("id", detail.Id));
            command.Parameters.Add(Parameter("fixture_id", fixtureId));
            command.Parameters.Add(BigIntParameter("lineup_id", NullIfZero(lineupId)));
            command.Parameters.Add(BigIntParameter("player_id", NullIfZero(playerId)));
            command.Parameters.Add(BigIntParameter("team_id", NullIfZero(teamId)));
            command.Parameters.Add(BigIntParameter("type_id", NullIfZero(detail.TypeId)));
            command.Parameters.Add(JsonbParameter("data", detail.Data));

            await command.ExecuteNonQueryAsync(cancellationToken);
            return true;
        }

        private static async Task<bool> UpsertFormationAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Formation formation,
            long fallbackFixtureId,
            CancellationToken cancellationToken)
        {
            if (formation.Id == 0 || formation.ParticipantId == 0)
            {
                return false;
            }

            var fixtureId = formation.FixtureId == 0 ? fallbackFixtureId : formation.FixtureId;
            if (fixtureId == 0)
            {
                return false;
            }

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into football.fixture_formations (
                    id,
                    fixture_id,
                    participant_id,
                    formation,
                    location,
                    last_synced_at)
                select
                    @id,
                    @fixture_id,
                    teams.id,
                    @formation,
                    @location,
                    now()
                from football.teams teams
                where teams.id = @participant_id
                on conflict (id) do update set
                    fixture_id = excluded.fixture_id,
                    participant_id = excluded.participant_id,
                    formation = excluded.formation,
                    location = excluded.location,
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("id", formation.Id));
            command.Parameters.Add(Parameter("fixture_id", fixtureId));
            command.Parameters.Add(Parameter("participant_id", formation.ParticipantId));
            command.Parameters.Add(TextParameter("formation", NullIfWhiteSpace(formation.TeamFormation)));
            command.Parameters.Add(TextParameter("location", NullIfWhiteSpace(formation.Location)));

            var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
            return affectedRows > 0;
        }

        private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                throw new InvalidOperationException(
                    "PostgreSQL connection string 'PreOddsApiPostgresDb' is required for fixture lineup/formation sync.");
            }

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }

        private static string? NullIfWhiteSpace(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static long? NullIfZero(long value)
        {
            return value == 0 ? null : value;
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

        private static NpgsqlParameter TextParameter(string name, string? value)
        {
            return new NpgsqlParameter(name, NpgsqlDbType.Text)
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
