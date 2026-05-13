using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Npgsql;
using PreOddsApi.WebApi.V3.Dtos;

namespace PreOddsApi.WebApi.V3.Data
{
    /// <summary>
    /// Read-only Postgres access for the fixture surface. Split across two
    /// files via partial class:
    ///   - PostgresFixtureReader.cs (this file): listing + single fixture +
    ///     shared helpers (OpenAsync, ReadNullable*, MapSummaryWithTeams).
    ///   - PostgresFixtureReader.Extras.cs: per-fixture extras (odds rates,
    ///     events, statistics, lineups, head-to-head).
    /// </summary>
    public sealed partial class PostgresFixtureReader : IFixtureReader
    {
        private readonly NpgsqlDataSource _dataSource;

        public PostgresFixtureReader(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public async Task<(IReadOnlyList<FixtureSummaryDto> Items, int Total)> GetFixturesAsync(
            DateTime? date,
            DateTime? fromDate,
            DateTime? toDate,
            long? leagueId,
            long? seasonId,
            long? teamId,
            long? stateId,
            int page,
            int perPage,
            CancellationToken ct = default)
        {
            var clauses = new List<string>();
            var parameters = new List<NpgsqlParameter>();

            if (date.HasValue)
            {
                clauses.Add("f.starting_at >= @date_from and f.starting_at < @date_to");
                parameters.Add(new NpgsqlParameter("date_from", date.Value.Date));
                parameters.Add(new NpgsqlParameter("date_to", date.Value.Date.AddDays(1)));
            }
            else
            {
                if (fromDate.HasValue)
                {
                    clauses.Add("f.starting_at >= @from_date");
                    parameters.Add(new NpgsqlParameter("from_date", fromDate.Value.Date));
                }
                if (toDate.HasValue)
                {
                    clauses.Add("f.starting_at < @to_date");
                    parameters.Add(new NpgsqlParameter("to_date", toDate.Value.Date.AddDays(1)));
                }
            }

            if (leagueId.HasValue)
            {
                clauses.Add("f.league_id = @league_id");
                parameters.Add(new NpgsqlParameter("league_id", leagueId.Value));
            }
            if (seasonId.HasValue)
            {
                clauses.Add("f.season_id = @season_id");
                parameters.Add(new NpgsqlParameter("season_id", seasonId.Value));
            }
            if (stateId.HasValue)
            {
                clauses.Add("f.state_id = @state_id");
                parameters.Add(new NpgsqlParameter("state_id", stateId.Value));
            }

            var teamJoin = string.Empty;
            if (teamId.HasValue)
            {
                teamJoin = "inner join football.fixture_participants p on p.fixture_id = f.id and p.team_id = @team_id";
                parameters.Add(new NpgsqlParameter("team_id", teamId.Value));
            }

            var where = clauses.Count > 0 ? "where " + string.Join(" and ", clauses) : string.Empty;

            var sql = $"""
                select f.id, f.name, f.league_id, f.season_id, f.stage_id, f.round_id,
                       f.state_id, f.venue_id, f.starting_at, f.has_odds, f.has_premium_odds,
                       f.length_minutes, f.result_info, f.leg, f.placeholder,
                       home_p.team_id as home_team_id,
                       home_t.name as home_team_name,
                       home_t.short_code as home_team_short_code,
                       home_t.image_path as home_team_image_path,
                       home_score.goals as home_score,
                       away_p.team_id as away_team_id,
                       away_t.name as away_team_name,
                       away_t.short_code as away_team_short_code,
                       away_t.image_path as away_team_image_path,
                       away_score.goals as away_score,
                       live_period.minutes as live_minute,
                       coalesce(home_cards.cnt, 0) as home_red_cards,
                       coalesce(away_cards.cnt, 0) as away_red_cards,
                       coalesce(home_var.active, false) as home_var_active,
                       coalesce(away_var.active, false) as away_var_active,
                       v.name as venue_name,
                       main_ref.name as referee_name,
                       count(*) over() as total_count
                from football.fixtures f
                {teamJoin}
                left join football.fixture_participants home_p
                    on home_p.fixture_id = f.id and home_p.location = 'home'
                left join football.teams home_t on home_t.id = home_p.team_id
                left join football.fixture_participants away_p
                    on away_p.fixture_id = f.id and away_p.location = 'away'
                left join football.teams away_t on away_t.id = away_p.team_id
                left join football.venues v on v.id = f.venue_id
                left join lateral (
                    -- Single referee per fixture — the first row by id
                    -- is the main centre official in every sample I've
                    -- spot-checked. Worth refining once SportMonks
                    -- documents type ids stably.
                    select r.name
                    from football.fixture_referees fr
                    join football.referees r on r.id = fr.referee_id
                    where fr.fixture_id = f.id
                    order by fr.referee_id
                    limit 1
                ) main_ref on true
                left join lateral (
                    select goals from football.fixture_scores
                    where fixture_id = f.id
                      and participant_location = 'home'
                      and description = 'CURRENT'
                    order by id desc
                    limit 1
                ) home_score on true
                left join lateral (
                    select goals from football.fixture_scores
                    where fixture_id = f.id
                      and participant_location = 'away'
                      and description = 'CURRENT'
                    order by id desc
                    limit 1
                ) away_score on true
                left join lateral (
                    select minutes from football.fixture_periods
                    where fixture_id = f.id and ticking = true
                    order by sort_order desc nulls last, id desc
                    limit 1
                ) live_period on true
                left join lateral (
                    select count(*)::int as cnt from football.fixture_events
                    where fixture_id = f.id
                      and participant_id = home_p.team_id
                      and type_id in (20, 21)
                ) home_cards on true
                left join lateral (
                    select count(*)::int as cnt from football.fixture_events
                    where fixture_id = f.id
                      and participant_id = away_p.team_id
                      and type_id in (20, 21)
                ) away_cards on true
                left join lateral (
                    -- VAR is "active" only while a freshly-inserted event row
                    -- is within its 90s window AND the fixture is currently
                    -- live. last_synced_at would lie here: every full sync
                    -- bumps it for historical rows too, leaving the badge
                    -- stuck on the home list for the rest of the match.
                    select exists(
                        select 1 from football.fixture_events
                        where fixture_id = f.id
                          and participant_id = home_p.team_id
                          and type_id in (10, 1697)
                          and created_at > now() - interval '90 seconds'
                          and f.state_id in (2, 3, 4, 6, 19, 20, 22, 25, 26)
                    ) as active
                ) home_var on true
                left join lateral (
                    select exists(
                        select 1 from football.fixture_events
                        where fixture_id = f.id
                          and participant_id = away_p.team_id
                          and type_id in (10, 1697)
                          and created_at > now() - interval '90 seconds'
                          and f.state_id in (2, 3, 4, 6, 19, 20, 22, 25, 26)
                    ) as active
                ) away_var on true
                {where}
                order by f.starting_at desc nulls last, f.id desc
                limit @limit offset @offset;
                """;

            parameters.Add(new NpgsqlParameter("limit", perPage));
            parameters.Add(new NpgsqlParameter("offset", (page - 1) * perPage));

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddRange(parameters.ToArray());

            var items = new List<FixtureSummaryDto>();
            var total = 0;

            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                if (total == 0)
                    total = reader.GetInt32(reader.GetOrdinal("total_count"));
                items.Add(MapSummaryWithTeams(reader));
            }

            return (items, total);
        }

        public async Task<FixtureDetailDto?> GetFixtureByIdAsync(long id, CancellationToken ct = default)
        {
            await using var connection = await OpenAsync(ct);

            FixtureSummaryDto? summary = null;

            await using (var command = new NpgsqlCommand(
                """
                select f.id, f.name, f.league_id, f.season_id, f.stage_id, f.round_id,
                       f.state_id, f.venue_id, f.starting_at, f.has_odds, f.has_premium_odds,
                       f.length_minutes, f.result_info, f.leg, f.placeholder,
                       home_p.team_id as home_team_id,
                       home_t.name as home_team_name,
                       home_t.short_code as home_team_short_code,
                       home_t.image_path as home_team_image_path,
                       home_score.goals as home_score,
                       away_p.team_id as away_team_id,
                       away_t.name as away_team_name,
                       away_t.short_code as away_team_short_code,
                       away_t.image_path as away_team_image_path,
                       away_score.goals as away_score,
                       live_period.minutes as live_minute,
                       coalesce(home_cards.cnt, 0) as home_red_cards,
                       coalesce(away_cards.cnt, 0) as away_red_cards,
                       coalesce(home_var.active, false) as home_var_active,
                       coalesce(away_var.active, false) as away_var_active,
                       v.name as venue_name,
                       main_ref.name as referee_name
                from football.fixtures f
                left join football.fixture_participants home_p
                    on home_p.fixture_id = f.id and home_p.location = 'home'
                left join football.teams home_t on home_t.id = home_p.team_id
                left join football.fixture_participants away_p
                    on away_p.fixture_id = f.id and away_p.location = 'away'
                left join football.teams away_t on away_t.id = away_p.team_id
                left join football.venues v on v.id = f.venue_id
                left join lateral (
                    select r.name
                    from football.fixture_referees fr
                    join football.referees r on r.id = fr.referee_id
                    where fr.fixture_id = f.id
                    order by fr.referee_id
                    limit 1
                ) main_ref on true
                left join lateral (
                    select goals from football.fixture_scores
                    where fixture_id = f.id
                      and participant_location = 'home'
                      and description = 'CURRENT'
                    order by id desc
                    limit 1
                ) home_score on true
                left join lateral (
                    select goals from football.fixture_scores
                    where fixture_id = f.id
                      and participant_location = 'away'
                      and description = 'CURRENT'
                    order by id desc
                    limit 1
                ) away_score on true
                left join lateral (
                    select minutes from football.fixture_periods
                    where fixture_id = f.id and ticking = true
                    order by sort_order desc nulls last, id desc
                    limit 1
                ) live_period on true
                left join lateral (
                    select count(*)::int as cnt from football.fixture_events
                    where fixture_id = f.id
                      and participant_id = home_p.team_id
                      and type_id in (20, 21)
                ) home_cards on true
                left join lateral (
                    select count(*)::int as cnt from football.fixture_events
                    where fixture_id = f.id
                      and participant_id = away_p.team_id
                      and type_id in (20, 21)
                ) away_cards on true
                left join lateral (
                    -- See the listing query above for why this is created_at,
                    -- not last_synced_at.
                    select exists(
                        select 1 from football.fixture_events
                        where fixture_id = f.id
                          and participant_id = home_p.team_id
                          and type_id in (10, 1697)
                          and created_at > now() - interval '90 seconds'
                          and f.state_id in (2, 3, 4, 6, 19, 20, 22, 25, 26)
                    ) as active
                ) home_var on true
                left join lateral (
                    select exists(
                        select 1 from football.fixture_events
                        where fixture_id = f.id
                          and participant_id = away_p.team_id
                          and type_id in (10, 1697)
                          and created_at > now() - interval '90 seconds'
                          and f.state_id in (2, 3, 4, 6, 19, 20, 22, 25, 26)
                    ) as active
                ) away_var on true
                where f.id = @id
                limit 1;
                """, connection))
            {
                command.Parameters.Add(new NpgsqlParameter("id", id));
                await using var reader = await command.ExecuteReaderAsync(ct);
                if (await reader.ReadAsync(ct))
                    summary = MapSummaryWithTeams(reader);
            }

            if (summary == null)
                return null;

            var participants = new List<FixtureParticipantDto>();
            await using (var command = new NpgsqlCommand(
                """
                select team_id, location, winner, position
                from football.fixture_participants
                where fixture_id = @fixture_id
                order by location, position;
                """, connection))
            {
                command.Parameters.Add(new NpgsqlParameter("fixture_id", id));
                await using var reader = await command.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    participants.Add(new FixtureParticipantDto
                    {
                        TeamId = reader.GetInt64(reader.GetOrdinal("team_id")),
                        Location = reader.GetString(reader.GetOrdinal("location")),
                        Winner = ReadNullableBool(reader, "winner"),
                        Position = ReadNullableInt(reader, "position")
                    });
                }
            }

            var scores = new List<FixtureScoreDto>();
            await using (var command = new NpgsqlCommand(
                """
                select id, type_id, participant_id, participant_location, description, goals
                from football.fixture_scores
                where fixture_id = @fixture_id
                order by type_id, participant_location;
                """, connection))
            {
                command.Parameters.Add(new NpgsqlParameter("fixture_id", id));
                await using var reader = await command.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    scores.Add(new FixtureScoreDto
                    {
                        Id = reader.GetInt64(reader.GetOrdinal("id")),
                        TypeId = ReadNullableLong(reader, "type_id"),
                        ParticipantId = ReadNullableLong(reader, "participant_id"),
                        ParticipantLocation = ReadNullableString(reader, "participant_location"),
                        Description = ReadNullableString(reader, "description"),
                        Goals = ReadNullableInt(reader, "goals")
                    });
                }
            }

            return new FixtureDetailDto
            {
                Fixture = summary,
                Participants = participants,
                Scores = scores
            };
        }


        private static decimal? ReadNullableDecimal(NpgsqlDataReader r, string column)
        {
            var i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? null : r.GetDecimal(i);
        }

        private static FixtureSummaryDto MapSummaryWithTeams(NpgsqlDataReader r) => new()
        {
            Id = r.GetInt64(r.GetOrdinal("id")),
            Name = ReadNullableString(r, "name"),
            LeagueId = r.GetInt64(r.GetOrdinal("league_id")),
            SeasonId = ReadNullableLong(r, "season_id"),
            StageId = ReadNullableLong(r, "stage_id"),
            RoundId = ReadNullableLong(r, "round_id"),
            StateId = ReadNullableLong(r, "state_id"),
            VenueId = ReadNullableLong(r, "venue_id"),
            StartingAt = ReadNullableDateTimeOffset(r, "starting_at"),
            HasOdds = r.GetBoolean(r.GetOrdinal("has_odds")),
            HasPremiumOdds = r.GetBoolean(r.GetOrdinal("has_premium_odds")),
            LengthMinutes = ReadNullableInt(r, "length_minutes"),
            ResultInfo = ReadNullableString(r, "result_info"),
            Leg = ReadNullableString(r, "leg"),
            Placeholder = r.GetBoolean(r.GetOrdinal("placeholder")),
            HomeTeamId = ReadNullableLong(r, "home_team_id"),
            HomeTeamName = ReadNullableString(r, "home_team_name"),
            HomeTeamShortCode = ReadNullableString(r, "home_team_short_code"),
            HomeTeamImagePath = ReadNullableString(r, "home_team_image_path"),
            HomeScore = ReadNullableInt(r, "home_score"),
            AwayTeamId = ReadNullableLong(r, "away_team_id"),
            AwayTeamName = ReadNullableString(r, "away_team_name"),
            AwayTeamShortCode = ReadNullableString(r, "away_team_short_code"),
            AwayTeamImagePath = ReadNullableString(r, "away_team_image_path"),
            AwayScore = ReadNullableInt(r, "away_score"),
            LiveMinute = ReadNullableInt(r, "live_minute"),
            HomeRedCards = ReadNullableInt(r, "home_red_cards"),
            AwayRedCards = ReadNullableInt(r, "away_red_cards"),
            HomeVarActive = ReadNullableBool(r, "home_var_active"),
            AwayVarActive = ReadNullableBool(r, "away_var_active"),
            VenueName = ReadNullableString(r, "venue_name"),
            RefereeName = ReadNullableString(r, "referee_name")
        };

        private Task<NpgsqlConnection> OpenAsync(CancellationToken ct)
            => _dataSource.OpenConnectionAsync(ct).AsTask();

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

        private static bool? ReadNullableBool(NpgsqlDataReader r, string column)
        {
            var i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? null : r.GetBoolean(i);
        }

        private static string? ReadNullableString(NpgsqlDataReader r, string column)
        {
            var i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? null : r.GetString(i);
        }

        private static DateTimeOffset? ReadNullableDateTimeOffset(NpgsqlDataReader r, string column)
        {
            var i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? null : r.GetFieldValue<DateTimeOffset>(i);
        }
    }
}
