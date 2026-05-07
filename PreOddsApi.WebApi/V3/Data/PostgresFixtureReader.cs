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
    public sealed class PostgresFixtureReader : IFixtureReader
    {
        private readonly string? _connectionString;

        public PostgresFixtureReader(IConfiguration configuration)
        {
            _connectionString = Environment.GetEnvironmentVariable("PREODDS_POSTGRES_CONNECTION")
                ?? configuration.GetConnectionString("PreOddsApiPostgresDb");
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
                       count(*) over() as total_count
                from football.fixtures f
                {teamJoin}
                left join football.fixture_participants home_p
                    on home_p.fixture_id = f.id and home_p.location = 'home'
                left join football.teams home_t on home_t.id = home_p.team_id
                left join football.fixture_participants away_p
                    on away_p.fixture_id = f.id and away_p.location = 'away'
                left join football.teams away_t on away_t.id = away_p.team_id
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
                       away_score.goals as away_score
                from football.fixtures f
                left join football.fixture_participants home_p
                    on home_p.fixture_id = f.id and home_p.location = 'home'
                left join football.teams home_t on home_t.id = home_p.team_id
                left join football.fixture_participants away_p
                    on away_p.fixture_id = f.id and away_p.location = 'away'
                left join football.teams away_t on away_t.id = away_p.team_id
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

        public async Task<IReadOnlyList<FixtureOddsRatesDto>> GetFixtureOddsRatesAsync(
            long fixtureId,
            long bookmakerId,
            IReadOnlyList<long> marketIds,
            string windowCode,
            CancellationToken ct = default)
        {
            const string sql = """
                select m.id as market_id,
                       m.name as market_name,
                       poc.label,
                       poc.value,
                       poc.total,
                       poc.handicap,
                       poc.participants,
                       poc.sort_order,
                       fs.win_count,
                       fs.lost_count,
                       fs.sample_count,
                       fs.winning_percent,
                       fs.earning_percent
                from odds.prematch_odds_current poc
                left join odds.markets m on m.id = poc.market_id
                left join analytics.fixture_signals fs
                    on fs.fixture_id    = poc.fixture_id
                   and fs.bookmaker_id  = poc.bookmaker_id
                   and fs.market_id     = poc.market_id
                   and fs.outcome_key   = poc.outcome_key
                   and fs.window_code   = @window_code
                   and fs.signal_type   = 'winning_rate'
                where poc.fixture_id   = @fixture_id
                  and poc.bookmaker_id = @bookmaker_id
                  and poc.market_id    = any(@market_ids)
                order by poc.market_id, poc.sort_order nulls last, poc.label;
                """;

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.Add(new NpgsqlParameter("fixture_id", fixtureId));
            command.Parameters.Add(new NpgsqlParameter("bookmaker_id", bookmakerId));
            command.Parameters.Add(new NpgsqlParameter("window_code", windowCode));
            command.Parameters.Add(
                new NpgsqlParameter("market_ids", marketIds.ToArray()));

            var grouped = new Dictionary<long, (string? Name, List<FixtureOddOutcomeDto> Outcomes)>();
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var marketId = reader.GetInt64(reader.GetOrdinal("market_id"));
                if (!grouped.TryGetValue(marketId, out var bucket))
                {
                    bucket = (ReadNullableString(reader, "market_name"), new List<FixtureOddOutcomeDto>());
                    grouped[marketId] = bucket;
                }

                var winCount = ReadNullableInt(reader, "win_count") ?? 0;
                var lostCount = ReadNullableInt(reader, "lost_count") ?? 0;

                bucket.Outcomes.Add(new FixtureOddOutcomeDto
                {
                    Label = reader.GetString(reader.GetOrdinal("label")),
                    Value = ReadNullableDecimal(reader, "value"),
                    Total = ReadNullableString(reader, "total"),
                    Handicap = ReadNullableString(reader, "handicap"),
                    Participants = ReadNullableString(reader, "participants"),
                    SortOrder = ReadNullableInt(reader, "sort_order"),
                    WinCount = winCount,
                    LostCount = lostCount,
                    SampleCount = ReadNullableInt(reader, "sample_count") ?? (winCount + lostCount),
                    WinningPercent = ReadNullableDecimal(reader, "winning_percent"),
                    EarningPercent = ReadNullableDecimal(reader, "earning_percent")
                });
            }

            var orderedIds = marketIds.Where(grouped.ContainsKey).ToList();
            return orderedIds
                .Select(id =>
                {
                    var (name, outcomes) = grouped[id];
                    return new FixtureOddsRatesDto
                    {
                        MarketId = id,
                        MarketName = name,
                        Outcomes = outcomes
                    };
                })
                .ToList();
        }

        public async Task<IReadOnlyList<FixtureEventDto>> GetFixtureEventsAsync(
            long fixtureId,
            CancellationToken ct = default)
        {
            const string sql = """
                select e.id, e.minute, e.extra_minute,
                       e.type_id, t.developer_name as type_code, t.name as type_name,
                       e.participant_id,
                       case
                           when fp_home.team_id = e.participant_id then 'home'
                           when fp_away.team_id = e.participant_id then 'away'
                       end as participant_location,
                       e.player_id, e.player_name, e.related_player_name,
                       e.result, e.info
                from football.fixture_events e
                left join catalog.types t on t.id = e.type_id
                left join football.fixture_participants fp_home
                    on fp_home.fixture_id = e.fixture_id and fp_home.location = 'home'
                left join football.fixture_participants fp_away
                    on fp_away.fixture_id = e.fixture_id and fp_away.location = 'away'
                where e.fixture_id = @fixture_id
                order by e.minute nulls last, e.extra_minute nulls last, e.id;
                """;

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.Add(new NpgsqlParameter("fixture_id", fixtureId));

            var items = new List<FixtureEventDto>();
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                items.Add(new FixtureEventDto
                {
                    Id = reader.GetInt64(reader.GetOrdinal("id")),
                    Minute = ReadNullableInt(reader, "minute"),
                    ExtraMinute = ReadNullableInt(reader, "extra_minute"),
                    TypeId = ReadNullableLong(reader, "type_id"),
                    TypeCode = ReadNullableString(reader, "type_code"),
                    TypeName = ReadNullableString(reader, "type_name"),
                    ParticipantId = ReadNullableLong(reader, "participant_id"),
                    ParticipantLocation = ReadNullableString(reader, "participant_location"),
                    PlayerId = ReadNullableLong(reader, "player_id"),
                    PlayerName = ReadNullableString(reader, "player_name"),
                    RelatedPlayerName = ReadNullableString(reader, "related_player_name"),
                    Result = ReadNullableString(reader, "result"),
                    Info = ReadNullableString(reader, "info")
                });
            }
            return items;
        }

        public async Task<IReadOnlyList<FixtureStatisticDto>> GetFixtureStatisticsAsync(
            long fixtureId,
            CancellationToken ct = default)
        {
            // Pivot per (fixture, type) into home/away values.
            const string sql = """
                with rows as (
                    select s.type_id, t.developer_name as type_code, t.name as type_name,
                           case
                               when fp_home.team_id = s.participant_id then 'home'
                               when fp_away.team_id = s.participant_id then 'away'
                               else coalesce(s.location, '')
                           end as side,
                           s.value
                    from football.fixture_statistics s
                    left join catalog.types t on t.id = s.type_id
                    left join football.fixture_participants fp_home
                        on fp_home.fixture_id = s.fixture_id and fp_home.location = 'home'
                    left join football.fixture_participants fp_away
                        on fp_away.fixture_id = s.fixture_id and fp_away.location = 'away'
                    where s.fixture_id = @fixture_id
                      and s.type_id is not null
                )
                select type_id, max(type_code) as type_code, max(type_name) as type_name,
                       max(value) filter (where side = 'home') as home_value,
                       max(value) filter (where side = 'away') as away_value
                from rows
                group by type_id
                order by type_id;
                """;

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.Add(new NpgsqlParameter("fixture_id", fixtureId));

            var items = new List<FixtureStatisticDto>();
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                items.Add(new FixtureStatisticDto
                {
                    TypeId = reader.GetInt64(reader.GetOrdinal("type_id")),
                    TypeCode = ReadNullableString(reader, "type_code"),
                    TypeName = ReadNullableString(reader, "type_name"),
                    HomeValue = ReadNullableDecimal(reader, "home_value"),
                    AwayValue = ReadNullableDecimal(reader, "away_value")
                });
            }
            return items;
        }

        public async Task<FixtureLineupsDto> GetFixtureLineupsAsync(
            long fixtureId,
            CancellationToken ct = default)
        {
            const string sql = """
                with parts as (
                    select team_id, location
                    from football.fixture_participants
                    where fixture_id = @fixture_id
                ),
                forms as (
                    select participant_id, formation, location
                    from football.fixture_formations
                    where fixture_id = @fixture_id
                )
                select l.player_id, l.player_name, l.jersey_number,
                       l.formation_field, l.formation_position,
                       l.team_id, p.location as side, t.developer_name as type_code,
                       pos.developer_name as position_code,
                       f.formation as formation
                from football.fixture_lineups l
                left join parts p on p.team_id = l.team_id
                left join catalog.types t on t.id = l.type_id
                left join catalog.types pos on pos.id = l.position_id
                left join forms f on f.participant_id = l.team_id
                where l.fixture_id = @fixture_id
                order by p.location nulls last,
                         (case when t.developer_name = 'LINEUP' then 0 else 1 end),
                         l.formation_position nulls last,
                         l.jersey_number nulls last;
                """;

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.Add(new NpgsqlParameter("fixture_id", fixtureId));

            var homeStarters = new List<FixtureLineupPlayerDto>();
            var homeBench = new List<FixtureLineupPlayerDto>();
            var awayStarters = new List<FixtureLineupPlayerDto>();
            var awayBench = new List<FixtureLineupPlayerDto>();
            long? homeTeamId = null;
            long? awayTeamId = null;
            string? homeFormation = null;
            string? awayFormation = null;

            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var side = ReadNullableString(reader, "side");
                var typeCode = ReadNullableString(reader, "type_code");
                var teamId = ReadNullableLong(reader, "team_id");
                var formation = ReadNullableString(reader, "formation");
                var player = new FixtureLineupPlayerDto
                {
                    PlayerId = ReadNullableLong(reader, "player_id"),
                    PlayerName = ReadNullableString(reader, "player_name"),
                    JerseyNumber = ReadNullableInt(reader, "jersey_number"),
                    FormationField = ReadNullableString(reader, "formation_field"),
                    FormationPosition = ReadNullableInt(reader, "formation_position"),
                    PositionCode = ReadNullableString(reader, "position_code")
                };
                var isStarter = string.Equals(typeCode, "LINEUP", System.StringComparison.OrdinalIgnoreCase);

                if (string.Equals(side, "home", System.StringComparison.OrdinalIgnoreCase))
                {
                    homeTeamId ??= teamId;
                    homeFormation ??= formation;
                    (isStarter ? homeStarters : homeBench).Add(player);
                }
                else if (string.Equals(side, "away", System.StringComparison.OrdinalIgnoreCase))
                {
                    awayTeamId ??= teamId;
                    awayFormation ??= formation;
                    (isStarter ? awayStarters : awayBench).Add(player);
                }
            }

            return new FixtureLineupsDto
            {
                Home = homeStarters.Count + homeBench.Count > 0
                    ? new FixtureTeamLineupDto
                    {
                        TeamId = homeTeamId,
                        Formation = homeFormation,
                        Starters = homeStarters,
                        Bench = homeBench
                    }
                    : null,
                Away = awayStarters.Count + awayBench.Count > 0
                    ? new FixtureTeamLineupDto
                    {
                        TeamId = awayTeamId,
                        Formation = awayFormation,
                        Starters = awayStarters,
                        Bench = awayBench
                    }
                    : null
            };
        }

        public async Task<IReadOnlyList<FixtureSummaryDto>> GetFixtureH2HAsync(
            long fixtureId,
            int limit,
            CancellationToken ct = default)
        {
            const string sql = """
                with this_match as (
                    select fp.team_id, fp.location, f.starting_at
                    from football.fixture_participants fp
                    join football.fixtures f on f.id = fp.fixture_id
                    where fp.fixture_id = @fixture_id
                ),
                pair as (
                    select max(team_id) filter (where location = 'home') as home_id,
                           max(team_id) filter (where location = 'away') as away_id,
                           min(starting_at) as ref_starting_at
                    from this_match
                ),
                shared as (
                    select fp.fixture_id
                    from football.fixture_participants fp
                    where fp.team_id in (select home_id from pair)
                       or fp.team_id in (select away_id from pair)
                    group by fp.fixture_id
                    having count(*) = 2
                       and bool_and(fp.team_id in (select home_id from pair)
                                    or fp.team_id in (select away_id from pair))
                )
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
                       away_score.goals as away_score
                from football.fixtures f
                join shared s on s.fixture_id = f.id
                cross join pair
                left join football.fixture_participants home_p
                    on home_p.fixture_id = f.id and home_p.location = 'home'
                left join football.teams home_t on home_t.id = home_p.team_id
                left join football.fixture_participants away_p
                    on away_p.fixture_id = f.id and away_p.location = 'away'
                left join football.teams away_t on away_t.id = away_p.team_id
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
                where f.id <> @fixture_id
                  and f.starting_at <= coalesce(pair.ref_starting_at, now())
                order by f.starting_at desc nulls last
                limit @limit;
                """;

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.Add(new NpgsqlParameter("fixture_id", fixtureId));
            command.Parameters.Add(new NpgsqlParameter("limit", limit));

            var items = new List<FixtureSummaryDto>();
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                items.Add(MapSummaryWithTeams(reader));
            return items;
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
            AwayScore = ReadNullableInt(r, "away_score")
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
