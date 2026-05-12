using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using PreOddsApi.WebApi.V3.Dtos;

namespace PreOddsApi.WebApi.V3.Data
{
    /// <summary>
    /// Per-fixture detail / extras: odds rates, events, statistics, lineups,
    /// head-to-head. Split out of the main reader so the file stays readable;
    /// they share state-less helpers (OpenAsync + ReadNullable*) defined on
    /// the partial class in PostgresFixtureReader.cs.
    /// </summary>
    public sealed partial class PostgresFixtureReader
    {
        public async Task<IReadOnlyList<FixtureOddsRatesDto>> GetFixtureOddsRatesAsync(
            long fixtureId,
            long bookmakerId,
            IReadOnlyList<long> marketIds,
            string windowCode,
            CancellationToken ct = default)
        {
            // Yol A: signals come from analytics.odd_analysis_snapshots —
            // outcome_key already follows "label:total:handicap:value(4dp)",
            // so we reconstruct the same key from current odds and JOIN on it.
            // The snapshot table has no fixture_id; the per-fixture filter
            // applies on the prematch_odds_current side and JOIN attaches the
            // shared per-outcome stat.
            //
            // When `marketIds` is empty we fall back to "every market that
            // has_winning_calculations" — same filter the analytics pipeline
            // uses, so we never return rows we have no signal for.
            var marketFilter = marketIds.Count > 0
                ? "and poc.market_id    = any(@market_ids)"
                : "and coalesce(m.has_winning_calculations, false) = true";

            // İKO sits on a per-(market, total-line, handicap) inverse-sum
            // so Over/Under 2.5 doesn't mix with Over/Under 3.5. The window
            // function runs over the same partition that defines a "betting
            // line" so each outcome gets its own no-vig probability.
            var sql = $"""
                with poc_with_iko as (
                    select poc.*,
                           round(
                               100.0 * (1.0 / poc.value::numeric) / nullif(
                                   sum(1.0 / poc.value::numeric) over (
                                       partition by poc.fixture_id, poc.bookmaker_id,
                                                    poc.market_id,
                                                    coalesce(nullif(poc.total, ''), '-'),
                                                    coalesce(nullif(poc.handicap, ''), '-')
                                   ),
                                   0
                               ),
                               4
                           ) as iko
                    from odds.prematch_odds_current poc
                    where poc.fixture_id   = @fixture_id
                      and poc.bookmaker_id = @bookmaker_id
                      and poc.value::numeric > 0
                )
                select m.id as market_id,
                       m.name as market_name,
                       poc.label,
                       poc.total,
                       poc.handicap,
                       poc.participants,
                       poc.sort_order,
                       poc.winning,
                       poc.value::numeric as odd_value,
                       poc.iko,
                       fs.win_count,
                       fs.lost_count,
                       (fs.win_count + fs.lost_count) as sample_count,
                       fs.winning_percent,
                       fs.earning_percent
                from poc_with_iko poc
                left join odds.markets m on m.id = poc.market_id
                left join analytics.odd_analysis_snapshots fs
                    on fs.bookmaker_id  = poc.bookmaker_id
                   and fs.market_id     = poc.market_id
                   and fs.outcome_key   = lower(coalesce(poc.label, ''))
                                          || ':' || coalesce(nullif(poc.total, ''), '-')
                                          || ':' || coalesce(nullif(poc.handicap, ''), '-')
                                          || ':' || to_char(poc.value::numeric, 'FM99999990.0000')
                   and fs.window_code   = @window_code
                   and fs.feed_type     = coalesce(poc.feed_type, 'standard')
                   and fs.as_of_date    = current_date
                where 1 = 1
                  {marketFilter}
                order by poc.market_id, poc.sort_order nulls last, poc.label;
                """;

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.Add(new NpgsqlParameter("fixture_id", fixtureId));
            command.Parameters.Add(new NpgsqlParameter("bookmaker_id", bookmakerId));
            command.Parameters.Add(new NpgsqlParameter("window_code", windowCode));
            if (marketIds.Count > 0)
            {
                command.Parameters.Add(
                    new NpgsqlParameter("market_ids", marketIds.ToArray()));
            }

            var grouped = new Dictionary<long, (string? Name, List<FixtureOddOutcomeDto> Outcomes)>();
            var orderSeen = new List<long>();
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var marketId = reader.GetInt64(reader.GetOrdinal("market_id"));
                if (!grouped.TryGetValue(marketId, out var bucket))
                {
                    bucket = (ReadNullableString(reader, "market_name"), new List<FixtureOddOutcomeDto>());
                    grouped[marketId] = bucket;
                    orderSeen.Add(marketId);
                }

                var winCount = ReadNullableInt(reader, "win_count") ?? 0;
                var lostCount = ReadNullableInt(reader, "lost_count") ?? 0;

                bucket.Outcomes.Add(new FixtureOddOutcomeDto
                {
                    Label = reader.GetString(reader.GetOrdinal("label")),
                    Total = ReadNullableString(reader, "total"),
                    Handicap = ReadNullableString(reader, "handicap"),
                    Participants = ReadNullableString(reader, "participants"),
                    SortOrder = ReadNullableInt(reader, "sort_order"),
                    Winning = ReadNullableBool(reader, "winning"),
                    // poc.value stores the raw odd as text; cast to numeric
                    // in the projection above so the Npgsql reader hands us
                    // a decimal instead of a string-of-decimal.
                    Value = ReadNullableDecimal(reader, "odd_value"),
                    Iko = ReadNullableDecimal(reader, "iko"),
                    WinCount = winCount,
                    LostCount = lostCount,
                    SampleCount = ReadNullableInt(reader, "sample_count") ?? (winCount + lostCount),
                    WinningPercent = ReadNullableDecimal(reader, "winning_percent"),
                    EarningPercent = ReadNullableDecimal(reader, "earning_percent")
                });
            }

            // Honour the caller's market order when they passed one; otherwise
            // fall back to the order rows arrived (already sorted by market_id).
            var orderedIds = marketIds.Count > 0
                ? marketIds.Where(grouped.ContainsKey).ToList()
                : orderSeen;
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
                       e.result, e.info, e.injured
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
                    Info = ReadNullableString(reader, "info"),
                    Injured = ReadNullableBool(reader, "injured")
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
                       away_score.goals as away_score,
                       live_period.minutes as live_minute,
                       coalesce(home_cards.cnt, 0) as home_red_cards,
                       coalesce(away_cards.cnt, 0) as away_red_cards,
                       coalesce(home_var.active, false) as home_var_active,
                       coalesce(away_var.active, false) as away_var_active,
                       v.name as venue_name,
                       main_ref.name as referee_name
                from football.fixtures f
                join shared s on s.fixture_id = f.id
                cross join pair
                left join football.fixture_participants home_p
                    on home_p.fixture_id = f.id and home_p.location = 'home'
                left join football.teams home_t on home_t.id = home_p.team_id
                left join football.fixture_participants away_p
                    on away_p.fixture_id = f.id and away_p.location = 'away'
                left join football.teams away_t on away_t.id = away_p.team_id
                left join football.venues v on v.id = f.venue_id
                left join lateral (
                    select r.name from football.fixture_referees fr
                    join football.referees r on r.id = fr.referee_id
                    where fr.fixture_id = f.id
                    order by fr.id
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
                    select exists(
                        select 1 from football.fixture_events
                        where fixture_id = f.id
                          and participant_id = home_p.team_id
                          and type_id in (10, 1697)
                          and last_synced_at > now() - interval '60 seconds'
                    ) as active
                ) home_var on true
                left join lateral (
                    select exists(
                        select 1 from football.fixture_events
                        where fixture_id = f.id
                          and participant_id = away_p.team_id
                          and type_id in (10, 1697)
                          and last_synced_at > now() - interval '60 seconds'
                    ) as active
                ) away_var on true
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
    }
}
