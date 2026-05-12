using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using PreOddsApi.WebApi.V3.Dtos;

namespace PreOddsApi.WebApi.V3.Data
{
    // Reads for the SportMonks Growth + trial-bundle data streams:
    // trends, match facts, weather, TV stations, value bets. Kept in
    // its own partial so the older readers stay untouched.
    public sealed partial class PostgresFixtureReader
    {
        public async Task<IReadOnlyList<FixtureTrendDto>> GetFixtureTrendsAsync(
            long fixtureId,
            CancellationToken ct = default)
        {
            // Trends arrive per (type, participant, minute). UI plots a single
            // series per type with home/away points side-tagged so the client
            // can split them or render a stacked chart without re-aggregating.
            const string sql = """
                select ft.type_id,
                       t.code as type_code,
                       t.name as type_name,
                       ft.minute,
                       case
                           when fp_home.team_id = ft.participant_id then 'home'
                           when fp_away.team_id = ft.participant_id then 'away'
                       end as side,
                       ft.value
                from football.fixture_trends ft
                left join catalog.types t on t.id = ft.type_id
                left join football.fixture_participants fp_home
                    on fp_home.fixture_id = ft.fixture_id and fp_home.location = 'home'
                left join football.fixture_participants fp_away
                    on fp_away.fixture_id = ft.fixture_id and fp_away.location = 'away'
                where ft.fixture_id = @fixture_id
                  and ft.type_id is not null
                order by ft.type_id, ft.minute nulls last;
                """;

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.Add(new NpgsqlParameter("fixture_id", fixtureId));

            var grouped = new Dictionary<long, (string? Code, string? Name, List<FixtureTrendPointDto> Points)>();
            var order = new List<long>();
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var typeId = reader.GetInt64(reader.GetOrdinal("type_id"));
                if (!grouped.TryGetValue(typeId, out var bucket))
                {
                    bucket = (
                        ReadNullableString(reader, "type_code"),
                        ReadNullableString(reader, "type_name"),
                        new List<FixtureTrendPointDto>());
                    grouped[typeId] = bucket;
                    order.Add(typeId);
                }
                bucket.Points.Add(new FixtureTrendPointDto
                {
                    Minute = ReadNullableInt(reader, "minute"),
                    Side = ReadNullableString(reader, "side"),
                    Value = ReadNullableDecimal(reader, "value")
                });
            }

            var result = new List<FixtureTrendDto>(grouped.Count);
            foreach (var id in order)
            {
                var (code, name, points) = grouped[id];
                result.Add(new FixtureTrendDto
                {
                    TypeId = id,
                    TypeCode = code,
                    TypeName = name,
                    Points = points
                });
            }
            return result;
        }

        public async Task<IReadOnlyList<FixtureMatchFactDto>> GetFixtureMatchFactsAsync(
            long fixtureId,
            int limit,
            CancellationToken ct = default)
        {
            // natural_language carries pre-written narrative ("X has 6 wins in
            // their last…") that is the only display-ready content; rows
            // without it are skipped. Same-category facts cluster so the UI
            // can section them; ordering inside a category mirrors capture.
            const string sql = """
                select mf.id,
                       mf.type_id,
                       t.name as type_name,
                       mf.category,
                       mf.scope,
                       mf.participant,
                       mf.natural_language
                from football.fixture_match_facts mf
                left join catalog.types t on t.id = mf.type_id
                where mf.fixture_id = @fixture_id
                  and mf.natural_language is not null
                  and length(trim(mf.natural_language)) > 0
                order by mf.category nulls last, mf.captured_at desc, mf.id
                limit @limit;
                """;

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.Add(new NpgsqlParameter("fixture_id", fixtureId));
            command.Parameters.Add(new NpgsqlParameter("limit", limit));

            var items = new List<FixtureMatchFactDto>();
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                items.Add(new FixtureMatchFactDto
                {
                    Id = reader.GetInt64(reader.GetOrdinal("id")),
                    TypeId = ReadNullableLong(reader, "type_id"),
                    TypeName = ReadNullableString(reader, "type_name"),
                    Category = ReadNullableString(reader, "category"),
                    Scope = ReadNullableString(reader, "scope"),
                    Participant = ReadNullableString(reader, "participant"),
                    NaturalLanguage = ReadNullableString(reader, "natural_language")
                });
            }
            return items;
        }

        public async Task<FixtureWeatherDto?> GetFixtureWeatherAsync(
            long fixtureId,
            CancellationToken ct = default)
        {
            // Most recent report by last_synced_at — actual report supersedes
            // any earlier forecast row for the same fixture.
            const string sql = """
                select temperature, feels_like, wind, humidity, pressure,
                       clouds, description, icon, metric
                from football.fixture_weather_reports
                where fixture_id = @fixture_id
                order by last_synced_at desc nulls last, id desc
                limit 1;
                """;

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.Add(new NpgsqlParameter("fixture_id", fixtureId));

            await using var reader = await command.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct)) return null;

            decimal? tempDay = null, tempEvening = null;
            decimal? windSpeed = null;
            int? windDirection = null;

            // SportMonks returns temperature/wind as JSON blobs (e.g.
            // { "day": 17.29, "evening": 16.4 }). Parse defensively so a
            // missing key just leaves the field null.
            var tempJson = ReadNullableString(reader, "temperature");
            if (!string.IsNullOrWhiteSpace(tempJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(tempJson);
                    if (doc.RootElement.TryGetProperty("day", out var d) && d.TryGetDecimal(out var dv))
                        tempDay = dv;
                    if (doc.RootElement.TryGetProperty("evening", out var e) && e.TryGetDecimal(out var ev))
                        tempEvening = ev;
                }
                catch (JsonException) { /* leave nulls */ }
            }

            var windJson = ReadNullableString(reader, "wind");
            if (!string.IsNullOrWhiteSpace(windJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(windJson);
                    if (doc.RootElement.TryGetProperty("speed", out var s) && s.TryGetDecimal(out var sv))
                        windSpeed = sv;
                    if (doc.RootElement.TryGetProperty("direction", out var dir) && dir.TryGetInt32(out var dv))
                        windDirection = dv;
                }
                catch (JsonException) { /* leave nulls */ }
            }

            return new FixtureWeatherDto
            {
                TemperatureDay = tempDay,
                TemperatureEvening = tempEvening,
                WindSpeed = windSpeed,
                WindDirection = windDirection,
                Humidity = ReadNullableString(reader, "humidity"),
                Pressure = ReadNullableInt(reader, "pressure"),
                Clouds = ReadNullableString(reader, "clouds"),
                Description = ReadNullableString(reader, "description"),
                Icon = ReadNullableString(reader, "icon"),
                Metric = ReadNullableString(reader, "metric")
            };
        }

        public async Task<IReadOnlyList<FixtureTvStationDto>> GetFixtureTvStationsAsync(
            long fixtureId,
            CancellationToken ct = default)
        {
            // Two real-world issues we filter for:
            //  - SportMonks's per-fixture `include=tvStations` ships only ids
            //    without names, so the writer stamps a `tv-station-{id}`
            //    placeholder. We hide those until the standalone
            //    `/tv-stations` reference sync fills the real name.
            //  - For top-tier matches the same include lists every
            //    rights-holder globally (600+ channels). Cap at 20 so the
            //    UI surfaces a digestible "where to watch" list rather than
            //    a worldwide broadcaster directory.
            const string sql = """
                select tv.id, tv.name, tv.url, tv.image_path
                from football.fixture_tv_stations fts
                join football.tv_stations tv on tv.id = fts.tv_station_id
                where fts.fixture_id = @fixture_id
                  and tv.name is not null
                  and tv.name not like 'tv-station-%'
                order by tv.name
                limit 20;
                """;

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.Add(new NpgsqlParameter("fixture_id", fixtureId));

            var items = new List<FixtureTvStationDto>();
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                items.Add(new FixtureTvStationDto
                {
                    Id = reader.GetInt64(reader.GetOrdinal("id")),
                    Name = ReadNullableString(reader, "name"),
                    Url = ReadNullableString(reader, "url"),
                    ImagePath = ReadNullableString(reader, "image_path")
                });
            }
            return items;
        }

        public async Task<FixtureSidelinedDto> GetFixtureSidelinedAsync(
            long fixtureId,
            CancellationToken ct = default)
        {
            // football.fixture_sidelined isn't populated yet (the per-
            // fixture sidelined sync is off in production), so we fall
            // back to "currently absent players on either fixture team":
            // sidelined_players rows with completed=false and an end_date
            // that hasn't passed the fixture's kickoff. Joining via
            // fixture_participants gives us the home/away split.
            const string sql = """
                with fixture_teams as (
                    select fp.team_id, fp.location
                    from football.fixture_participants fp
                    where fp.fixture_id = @fixture_id
                ),
                fixture_meta as (
                    select coalesce(f.starting_at, now()) as kickoff
                    from football.fixtures f
                    where f.id = @fixture_id
                )
                select ft.location,
                       sp.player_id,
                       p.name             as player_name,
                       p.image_path       as player_image_path,
                       pos.developer_name as position_code,
                       sp.category,
                       cause.name         as reason,
                       -- end_date is a Postgres DATE; cast to timestamptz
                       -- so Npgsql hands us a DateTimeOffset the DTO can
                       -- accept without a custom converter.
                       (sp.end_date)::timestamptz as end_date,
                       sp.games_missed
                from football.sidelined_players sp
                join fixture_teams ft on ft.team_id = sp.team_id
                cross join fixture_meta fm
                left join football.players p on p.id = sp.player_id
                left join catalog.types pos on pos.id = p.position_id
                left join catalog.types cause on cause.id = sp.type_id
                where sp.completed = false
                  and (sp.end_date is null or sp.end_date >= (fm.kickoff at time zone 'UTC')::date)
                order by ft.location, sp.end_date nulls last, sp.id;
                """;

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.Add(new NpgsqlParameter("fixture_id", fixtureId));

            var home = new List<FixtureSidelinedItemDto>();
            var away = new List<FixtureSidelinedItemDto>();
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var item = new FixtureSidelinedItemDto
                {
                    PlayerId = ReadNullableLong(reader, "player_id"),
                    PlayerName = ReadNullableString(reader, "player_name"),
                    PlayerImagePath = ReadNullableString(reader, "player_image_path"),
                    PositionCode = ReadNullableString(reader, "position_code"),
                    Category = ReadNullableString(reader, "category"),
                    Reason = ReadNullableString(reader, "reason"),
                    EndDate = ReadNullableDateTimeOffset(reader, "end_date"),
                    GamesMissed = ReadNullableInt(reader, "games_missed")
                };
                var location = ReadNullableString(reader, "location") ?? string.Empty;
                if (string.Equals(location, "home", System.StringComparison.OrdinalIgnoreCase))
                    home.Add(item);
                else if (string.Equals(location, "away", System.StringComparison.OrdinalIgnoreCase))
                    away.Add(item);
            }
            return new FixtureSidelinedDto { Home = home, Away = away };
        }

        public async Task<FixtureExpectedGoalsDto?> GetFixtureExpectedGoalsAsync(
            long fixtureId,
            CancellationToken ct = default)
        {
            // Cumulative xG per side. SportMonks ships one row per (team
            // × type_id) where the row's `value` is the running total at
            // the latest sync; sum across types so we capture both base
            // xG and any "open-play vs set-piece" splits without making
            // the client juggle type ids.
            const string sql = """
                select coalesce(sum(value) filter (where location = 'home'), 0) as home,
                       coalesce(sum(value) filter (where location = 'away'), 0) as away,
                       count(*)                                                 as row_count
                from football.fixture_expected_goals
                where fixture_id = @fixture_id;
                """;
            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.Add(new NpgsqlParameter("fixture_id", fixtureId));

            await using var reader = await command.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct)) return null;
            var rowCount = reader.GetInt64(reader.GetOrdinal("row_count"));
            if (rowCount == 0) return null;
            return new FixtureExpectedGoalsDto
            {
                Home = ReadNullableDecimal(reader, "home"),
                Away = ReadNullableDecimal(reader, "away")
            };
        }

        public async Task<IReadOnlyList<FixtureValueBetDto>> GetFixtureValueBetsAsync(
            long fixtureId,
            CancellationToken ct = default)
        {
            // The is_value=true index is partial so this filter is essentially
            // free; non-value rows are noise from SportMonks's broader
            // exhaust and would dilute the recommendation badge.
            const string sql = """
                select vb.id, vb.type_id, t.name as type_name,
                       vb.bet, vb.bookmaker, vb.fair_odd, vb.odd, vb.stake, vb.is_value
                from analytics.sportmonks_value_bets vb
                left join catalog.types t on t.id = vb.type_id
                where vb.fixture_id = @fixture_id
                order by vb.is_value desc nulls last, vb.stake desc nulls last, vb.id desc;
                """;

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.Add(new NpgsqlParameter("fixture_id", fixtureId));

            var items = new List<FixtureValueBetDto>();
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                items.Add(new FixtureValueBetDto
                {
                    Id = reader.GetInt64(reader.GetOrdinal("id")),
                    TypeId = ReadNullableLong(reader, "type_id"),
                    TypeName = ReadNullableString(reader, "type_name"),
                    Bet = ReadNullableString(reader, "bet"),
                    Bookmaker = ReadNullableString(reader, "bookmaker"),
                    FairOdd = ReadNullableDecimal(reader, "fair_odd"),
                    Odd = ReadNullableDecimal(reader, "odd"),
                    Stake = ReadNullableDecimal(reader, "stake"),
                    IsValue = ReadNullableBool(reader, "is_value")
                });
            }
            return items;
        }
    }
}
