using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Npgsql;
using PreOddsApi.WebApi.V3.Dtos;

namespace PreOddsApi.WebApi.V3.Data
{
    public sealed class PostgresStandingsNewsReader : IStandingsNewsReader
    {
        private readonly string? _connectionString;

        public PostgresStandingsNewsReader(IConfiguration configuration)
        {
            _connectionString = Environment.GetEnvironmentVariable("PREODDS_POSTGRES_CONNECTION")
                ?? configuration.GetConnectionString("PreOddsApiPostgresDb");
        }

        public async Task<(IReadOnlyList<StandingDto> Items, int Total)> GetStandingsAsync(
            long? seasonId,
            long? leagueId,
            long? stageId,
            long? groupId,
            long? roundId,
            int page,
            int perPage,
            CancellationToken ct = default)
        {
            var clauses = new List<string>();
            var parameters = new List<NpgsqlParameter>();

            if (seasonId.HasValue) { clauses.Add("season_id = @season_id"); parameters.Add(new NpgsqlParameter("season_id", seasonId.Value)); }
            if (leagueId.HasValue) { clauses.Add("league_id = @league_id"); parameters.Add(new NpgsqlParameter("league_id", leagueId.Value)); }
            if (stageId.HasValue) { clauses.Add("stage_id = @stage_id"); parameters.Add(new NpgsqlParameter("stage_id", stageId.Value)); }
            if (groupId.HasValue) { clauses.Add("group_id = @group_id"); parameters.Add(new NpgsqlParameter("group_id", groupId.Value)); }
            if (roundId.HasValue) { clauses.Add("round_id = @round_id"); parameters.Add(new NpgsqlParameter("round_id", roundId.Value)); }

            var where = clauses.Count > 0 ? "where " + string.Join(" and ", clauses) : string.Empty;

            var sql = $"""
                select id, participant_id, league_id, season_id, stage_id, group_id, round_id,
                       position, result, points,
                       count(*) over() as total_count
                from competition.standings
                {where}
                order by stage_id nulls last, group_id nulls last, position nulls last
                limit @limit offset @offset;
                """;

            parameters.Add(new NpgsqlParameter("limit", perPage));
            parameters.Add(new NpgsqlParameter("offset", (page - 1) * perPage));

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddRange(parameters.ToArray());

            var items = new List<StandingDto>();
            var total = 0;

            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                if (total == 0)
                    total = reader.GetInt32(reader.GetOrdinal("total_count"));

                items.Add(new StandingDto
                {
                    Id = reader.GetInt64(reader.GetOrdinal("id")),
                    ParticipantId = ReadNullableLong(reader, "participant_id"),
                    LeagueId = ReadNullableLong(reader, "league_id"),
                    SeasonId = ReadNullableLong(reader, "season_id"),
                    StageId = ReadNullableLong(reader, "stage_id"),
                    GroupId = ReadNullableLong(reader, "group_id"),
                    RoundId = ReadNullableLong(reader, "round_id"),
                    Position = ReadNullableInt(reader, "position"),
                    Result = ReadNullableString(reader, "result"),
                    Points = ReadNullableInt(reader, "points")
                });
            }

            return (items, total);
        }

        public async Task<IReadOnlyList<LeagueTableRowDto>> GetLeagueTableAsync(
            long? leagueId,
            long? seasonId,
            long? stageId,
            CancellationToken ct = default)
        {
            // SportMonks splits a season into multiple "stages" (regular,
            // championship round, relegation round). The mobile detail screen
            // wants ONE table — the full league table — so when no stage_id
            // is given we pick the (stage_id, group_id, round_id) tuple with
            // the most teams; that's the regular-season league standing.
            var clauses = new List<string>();
            var parameters = new List<NpgsqlParameter>();

            if (leagueId.HasValue)
            {
                clauses.Add("s.league_id = @league_id");
                parameters.Add(new NpgsqlParameter("league_id", leagueId.Value));
            }
            if (seasonId.HasValue)
            {
                clauses.Add("s.season_id = @season_id");
                parameters.Add(new NpgsqlParameter("season_id", seasonId.Value));
            }
            if (stageId.HasValue)
            {
                clauses.Add("s.stage_id = @stage_id");
                parameters.Add(new NpgsqlParameter("stage_id", stageId.Value));
            }

            var where = clauses.Count > 0 ? "where " + string.Join(" and ", clauses) : string.Empty;

            // Pivot standing_details on developer_name so each team's row is
            // one logical record. Pick the largest grouping so we always
            // return the full league table even if the fixture sits inside
            // a smaller championship/relegation round.
            var sql = $"""
                with target as (
                    select stage_id, group_id, round_id, count(*) as team_count
                    from competition.standings s
                    {where}
                    group by stage_id, group_id, round_id
                    order by team_count desc, max(s.last_synced_at) desc
                    limit 1
                ),
                rows as (
                    select s.id, s.participant_id, s.position, s.points,
                           t.name as team_name, t.image_path as team_image_path
                    from competition.standings s
                    join target on coalesce(s.stage_id, -1) = coalesce(target.stage_id, -1)
                               and coalesce(s.group_id, -1) = coalesce(target.group_id, -1)
                               and coalesce(s.round_id, -1) = coalesce(target.round_id, -1)
                    left join football.teams t on t.id = s.participant_id
                    {(where.Length > 0 ? where : string.Empty)}
                )
                select r.participant_id as team_id, r.team_name, r.team_image_path,
                       r.position, r.points,
                       coalesce(max(d.value) filter (where ty.developer_name = 'OVERALL_MATCHES'), 0) as played,
                       coalesce(max(d.value) filter (where ty.developer_name = 'OVERALL_WINS'), 0) as wins,
                       coalesce(max(d.value) filter (where ty.developer_name = 'OVERALL_DRAWS'), 0) as draws,
                       coalesce(max(d.value) filter (where ty.developer_name = 'OVERALL_LOST'), 0) as losses,
                       coalesce(max(d.value) filter (where ty.developer_name = 'OVERALL_SCORED'), 0) as goals_for,
                       coalesce(max(d.value) filter (where ty.developer_name = 'OVERALL_CONCEDED'), 0) as goals_against,
                       coalesce(max(d.value) filter (where ty.developer_name = 'OVERALL_GOAL_DIFFERENCE'), 0) as goal_difference
                from rows r
                left join competition.standing_details d on d.standing_id = r.id
                left join catalog.types ty on ty.id = d.type_id
                group by r.participant_id, r.team_name, r.team_image_path, r.position, r.points
                order by r.position nulls last, r.points desc nulls last;
                """;

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddRange(parameters.ToArray());

            var items = new List<LeagueTableRowDto>();
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                items.Add(new LeagueTableRowDto
                {
                    TeamId = ReadNullableLong(reader, "team_id"),
                    TeamName = ReadNullableString(reader, "team_name"),
                    TeamImagePath = ReadNullableString(reader, "team_image_path"),
                    Position = ReadNullableInt(reader, "position"),
                    Points = ReadNullableInt(reader, "points") ?? 0,
                    Played = reader.GetInt32(reader.GetOrdinal("played")),
                    Wins = reader.GetInt32(reader.GetOrdinal("wins")),
                    Draws = reader.GetInt32(reader.GetOrdinal("draws")),
                    Losses = reader.GetInt32(reader.GetOrdinal("losses")),
                    GoalsFor = reader.GetInt32(reader.GetOrdinal("goals_for")),
                    GoalsAgainst = reader.GetInt32(reader.GetOrdinal("goals_against")),
                    GoalDifference = reader.GetInt32(reader.GetOrdinal("goal_difference")),
                });
            }

            return items;
        }

        public async Task<(IReadOnlyList<NewsSummaryDto> Items, int Total)> GetNewsAsync(
            long? fixtureId,
            long? leagueId,
            string? type,
            int page,
            int perPage,
            CancellationToken ct = default)
        {
            var clauses = new List<string>();
            var parameters = new List<NpgsqlParameter>();

            if (fixtureId.HasValue) { clauses.Add("fixture_id = @fixture_id"); parameters.Add(new NpgsqlParameter("fixture_id", fixtureId.Value)); }
            if (leagueId.HasValue) { clauses.Add("league_id = @league_id"); parameters.Add(new NpgsqlParameter("league_id", leagueId.Value)); }
            if (!string.IsNullOrWhiteSpace(type)) { clauses.Add("type = @type"); parameters.Add(new NpgsqlParameter("type", type.Trim())); }

            var where = clauses.Count > 0 ? "where " + string.Join(" and ", clauses) : string.Empty;

            var sql = $"""
                select id, fixture_id, league_id, title, type, created_at,
                       count(*) over() as total_count
                from football.news
                {where}
                order by created_at desc, id desc
                limit @limit offset @offset;
                """;

            parameters.Add(new NpgsqlParameter("limit", perPage));
            parameters.Add(new NpgsqlParameter("offset", (page - 1) * perPage));

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddRange(parameters.ToArray());

            var items = new List<NewsSummaryDto>();
            var total = 0;

            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                if (total == 0)
                    total = reader.GetInt32(reader.GetOrdinal("total_count"));

                items.Add(MapNewsSummary(reader));
            }

            return (items, total);
        }

        public async Task<NewsDetailDto?> GetNewsByIdAsync(long id, CancellationToken ct = default)
        {
            await using var connection = await OpenAsync(ct);

            NewsSummaryDto? summary = null;

            await using (var command = new NpgsqlCommand(
                """
                select id, fixture_id, league_id, title, type, created_at
                from football.news
                where id = @id
                limit 1;
                """, connection))
            {
                command.Parameters.Add(new NpgsqlParameter("id", id));
                await using var reader = await command.ExecuteReaderAsync(ct);
                if (await reader.ReadAsync(ct))
                    summary = MapNewsSummary(reader);
            }

            if (summary == null)
                return null;

            var lines = new List<NewsLineDto>();
            await using (var command = new NpgsqlCommand(
                """
                select id, text, type, sort_order
                from football.news_lines
                where news_id = @news_id
                order by sort_order nulls last, id;
                """, connection))
            {
                command.Parameters.Add(new NpgsqlParameter("news_id", id));
                await using var reader = await command.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    lines.Add(new NewsLineDto
                    {
                        Id = reader.GetInt64(reader.GetOrdinal("id")),
                        Text = reader.GetString(reader.GetOrdinal("text")),
                        Type = ReadNullableString(reader, "type"),
                        SortOrder = ReadNullableInt(reader, "sort_order")
                    });
                }
            }

            return new NewsDetailDto
            {
                News = summary,
                Lines = lines
            };
        }

        private static NewsSummaryDto MapNewsSummary(NpgsqlDataReader r) => new()
        {
            Id = r.GetInt64(r.GetOrdinal("id")),
            FixtureId = ReadNullableLong(r, "fixture_id"),
            LeagueId = ReadNullableLong(r, "league_id"),
            Title = r.GetString(r.GetOrdinal("title")),
            Type = ReadNullableString(r, "type"),
            CreatedAt = r.GetFieldValue<DateTimeOffset>(r.GetOrdinal("created_at"))
        };

        private async Task<NpgsqlConnection> OpenAsync(CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
                throw new InvalidOperationException(
                    "PostgreSQL connection string 'PreOddsApiPostgresDb' is required.");

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(ct);
            return connection;
        }

        private static long? ReadNullableLong(NpgsqlDataReader r, string column)
        {
            var i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? null : r.GetInt64(i);
        }

        private static int? ReadNullableInt(NpgsqlDataReader r, string column)
        {
            var i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? null : r.GetInt32(i);
        }

        private static string? ReadNullableString(NpgsqlDataReader r, string column)
        {
            var i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? null : r.GetString(i);
        }
    }
}
