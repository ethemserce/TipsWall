using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Npgsql;
using PreOddsApi.WebApi.V3.Dtos;

namespace PreOddsApi.WebApi.V3.Data
{
    public sealed class PostgresPlayerReader : IPlayerReader
    {
        private readonly string? _connectionString;

        public PostgresPlayerReader(IConfiguration configuration)
        {
            // Mirror the env-or-config pattern used by every other reader
            // in this project (PostgresAnalyticsEngine /
            // PostgresAccountPurgeService). Worker host injects via
            // PREODDS_POSTGRES_CONNECTION; ASP.NET via ConnectionStrings.
            _connectionString =
                Environment.GetEnvironmentVariable("PREODDS_POSTGRES_CONNECTION")
                ?? configuration.GetConnectionString("PreOddsApiPostgresDb");
        }

        public async Task<PlayerDto?> GetPlayerByIdAsync(long id, CancellationToken ct = default)
        {
            // Join the latest team_squads row by max(season_id), pulling
            // current team + jersey + captain status in the same shot.
            // catalog.types stringifies position_id into a UI-friendly
            // code (GOALKEEPER / DEFENDER / ...).
            const string sql = """
                select p.id,
                       p.name, p.display_name, p.first_name, p.last_name,
                       p.image_path, p.date_of_birth,
                       p.nationality_id, p.country_id,
                       p.height, p.weight,
                       p.position_id, t.developer_name as position_code,
                       p.gender,
                       sq.team_id           as current_team_id,
                       tm.name              as current_team_name,
                       tm.image_path        as current_team_image_path,
                       sq.jersey_number     as current_jersey_number,
                       sq.captain           as current_captain
                from football.players p
                left join catalog.types t on t.id = p.position_id
                left join lateral (
                    select team_id, jersey_number, captain, season_id
                    from football.team_squads
                    where player_id = p.id
                    order by season_id desc nulls last
                    limit 1
                ) sq on true
                left join football.teams tm on tm.id = sq.team_id
                where p.id = @id
                limit 1;
                """;

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.Add(new NpgsqlParameter("id", id));

            await using var reader = await command.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
                return null;

            return new PlayerDto
            {
                Id = reader.GetInt64(reader.GetOrdinal("id")),
                Name = ReadString(reader, "name") ?? string.Empty,
                DisplayName = ReadString(reader, "display_name"),
                FirstName = ReadString(reader, "first_name"),
                LastName = ReadString(reader, "last_name"),
                ImagePath = ReadString(reader, "image_path"),
                DateOfBirth = ReadDate(reader, "date_of_birth"),
                NationalityId = ReadLong(reader, "nationality_id"),
                CountryId = ReadLong(reader, "country_id"),
                Height = ReadInt(reader, "height"),
                Weight = ReadInt(reader, "weight"),
                PositionId = ReadLong(reader, "position_id"),
                PositionCode = ReadString(reader, "position_code"),
                Gender = ReadString(reader, "gender"),
                CurrentTeamId = ReadLong(reader, "current_team_id"),
                CurrentTeamName = ReadString(reader, "current_team_name"),
                CurrentTeamImagePath = ReadString(reader, "current_team_image_path"),
                CurrentJerseyNumber = ReadInt(reader, "current_jersey_number"),
                CurrentCaptain = ReadBool(reader, "current_captain"),
            };
        }

        public async Task<IReadOnlyList<PlayerSeasonStatsDto>> GetPlayerSeasonStatsAsync(
            long playerId, long? seasonId, CancellationToken ct = default)
        {
            // Latest as_of_date per (league, season, team). The nightly
            // refresh writes a single row per day; older seasons stick
            // at the last refresh that included them.
            var seasonFilter = seasonId.HasValue ? "and season_id = @season_id" : "";
            var sql = $"""
                with ranked as (
                    select s.*,
                           row_number() over (
                               partition by league_id, season_id, team_id
                               order by as_of_date desc
                           ) as rn
                    from analytics.season_player_stats s
                    where player_id = @player_id
                      and fixture_scope = 'all'
                      {seasonFilter}
                )
                select r.league_id, lg.name as league_name,
                       r.season_id, se.name as season_name,
                       r.team_id, tm.name as team_name, tm.image_path as team_image_path,
                       r.as_of_date, r.fixture_scope,
                       r.matches_played, r.matches_started,
                       r.matches_subbed_in, r.matches_subbed_out,
                       r.minutes_played,
                       r.goals, r.assists, r.own_goals,
                       r.penalties_scored, r.penalties_missed,
                       r.yellow_cards, r.red_cards
                from ranked r
                left join competition.leagues lg on lg.id = r.league_id
                left join competition.seasons se on se.id = r.season_id
                left join football.teams tm on tm.id = r.team_id
                where r.rn = 1
                order by r.as_of_date desc, r.league_id;
                """;

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.Add(new NpgsqlParameter("player_id", playerId));
            if (seasonId.HasValue)
                command.Parameters.Add(new NpgsqlParameter("season_id", seasonId.Value));

            var items = new List<PlayerSeasonStatsDto>();
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                items.Add(new PlayerSeasonStatsDto
                {
                    LeagueId = reader.GetInt64(reader.GetOrdinal("league_id")),
                    LeagueName = ReadString(reader, "league_name"),
                    SeasonId = reader.GetInt64(reader.GetOrdinal("season_id")),
                    SeasonName = ReadString(reader, "season_name"),
                    TeamId = reader.GetInt64(reader.GetOrdinal("team_id")),
                    TeamName = ReadString(reader, "team_name"),
                    TeamImagePath = ReadString(reader, "team_image_path"),
                    AsOfDate = reader.GetDateTime(reader.GetOrdinal("as_of_date")),
                    FixtureScope = reader.GetString(reader.GetOrdinal("fixture_scope")),
                    MatchesPlayed = ReadInt(reader, "matches_played"),
                    MatchesStarted = ReadInt(reader, "matches_started"),
                    MatchesSubbedIn = ReadInt(reader, "matches_subbed_in"),
                    MatchesSubbedOut = ReadInt(reader, "matches_subbed_out"),
                    MinutesPlayed = ReadInt(reader, "minutes_played"),
                    Goals = ReadInt(reader, "goals"),
                    Assists = ReadInt(reader, "assists"),
                    OwnGoals = ReadInt(reader, "own_goals"),
                    PenaltiesScored = ReadInt(reader, "penalties_scored"),
                    PenaltiesMissed = ReadInt(reader, "penalties_missed"),
                    YellowCards = ReadInt(reader, "yellow_cards"),
                    RedCards = ReadInt(reader, "red_cards"),
                });
            }
            return items;
        }

        private async Task<NpgsqlConnection> OpenAsync(CancellationToken ct)
        {
            var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(ct);
            return conn;
        }

        // Shared NULL-aware readers. Duplicated locally instead of
        // reaching into the reference reader's privates — keeps this
        // file self-contained for future extraction into its own DLL.
        private static string? ReadString(NpgsqlDataReader r, string column)
        {
            var i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? null : r.GetString(i);
        }

        private static int? ReadInt(NpgsqlDataReader r, string column)
        {
            var i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? null : r.GetInt32(i);
        }

        private static long? ReadLong(NpgsqlDataReader r, string column)
        {
            var i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? null : r.GetInt64(i);
        }

        private static bool? ReadBool(NpgsqlDataReader r, string column)
        {
            var i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? null : r.GetBoolean(i);
        }

        private static DateTime? ReadDate(NpgsqlDataReader r, string column)
        {
            var i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? null : r.GetDateTime(i);
        }
    }
}
