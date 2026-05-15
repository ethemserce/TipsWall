using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Npgsql;
using PreOddsApi.WebApi.V3.Dtos;

namespace PreOddsApi.WebApi.V3.Data
{
    public sealed class PostgresReferenceDataReader : IReferenceDataReader
    {
        private readonly NpgsqlDataSource _dataSource;

        public PostgresReferenceDataReader(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public async Task<(IReadOnlyList<CountryDto> Items, int Total)> GetCountriesAsync(
            long? continentId, string? search, string? iso2, int page, int perPage, CancellationToken ct = default)
        {
            var (where, parameters) = BuildWhere(filters =>
            {
                if (continentId.HasValue)
                    filters.Add("continent_id = @continent_id", new NpgsqlParameter("continent_id", continentId.Value));
                if (!string.IsNullOrWhiteSpace(search))
                    filters.Add("name ilike @search", new NpgsqlParameter("search", $"%{search.Trim()}%"));
                if (!string.IsNullOrWhiteSpace(iso2))
                    filters.Add("lower(iso2) = lower(@iso2)", new NpgsqlParameter("iso2", iso2.Trim()));
            });

            var sql = $"""
                select id, continent_id, name, official_name, iso2, iso3, image_path,
                       count(*) over() as total_count
                from catalog.countries
                {where}
                order by name
                limit @limit offset @offset;
                """;

            return await ReadPagedAsync(sql, parameters, page, perPage,
                r => new CountryDto
                {
                    Id = r.GetInt64(r.GetOrdinal("id")),
                    ContinentId = r.GetInt64(r.GetOrdinal("continent_id")),
                    Name = r.GetString(r.GetOrdinal("name")),
                    OfficialName = ReadNullableString(r, "official_name"),
                    Iso2 = ReadNullableString(r, "iso2"),
                    Iso3 = ReadNullableString(r, "iso3"),
                    ImagePath = ReadNullableString(r, "image_path")
                }, ct);
        }

        public async Task<CountryDto?> GetCountryByIdAsync(long id, CancellationToken ct = default)
        {
            const string sql = """
                select id, continent_id, name, official_name, iso2, iso3, image_path
                from catalog.countries
                where id = @id
                limit 1;
                """;

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.Add(new NpgsqlParameter("id", id));

            await using var reader = await command.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
                return null;

            return new CountryDto
            {
                Id = reader.GetInt64(reader.GetOrdinal("id")),
                ContinentId = reader.GetInt64(reader.GetOrdinal("continent_id")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                OfficialName = ReadNullableString(reader, "official_name"),
                Iso2 = ReadNullableString(reader, "iso2"),
                Iso3 = ReadNullableString(reader, "iso3"),
                ImagePath = ReadNullableString(reader, "image_path")
            };
        }

        public async Task<IReadOnlyList<ContinentDto>> GetContinentsAsync(CancellationToken ct = default)
        {
            const string sql = "select id, name, code from catalog.continents order by name;";

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);

            var items = new List<ContinentDto>();
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                items.Add(new ContinentDto
                {
                    Id = reader.GetInt64(reader.GetOrdinal("id")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    Code = ReadNullableString(reader, "code")
                });
            }

            return items;
        }

        public async Task<ContinentDto?> GetContinentByIdAsync(long id, CancellationToken ct = default)
        {
            const string sql = "select id, name, code from catalog.continents where id = @id limit 1;";

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.Add(new NpgsqlParameter("id", id));

            await using var reader = await command.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
                return null;

            return new ContinentDto
            {
                Id = reader.GetInt64(reader.GetOrdinal("id")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                Code = ReadNullableString(reader, "code")
            };
        }

        public async Task<(IReadOnlyList<LeagueDto> Items, int Total)> GetLeaguesAsync(
            long? countryId, bool? active, string? search, int page, int perPage, CancellationToken ct = default)
        {
            var (where, parameters) = BuildWhere(filters =>
            {
                if (countryId.HasValue)
                    filters.Add("country_id = @country_id", new NpgsqlParameter("country_id", countryId.Value));
                if (active.HasValue)
                    filters.Add("active = @active", new NpgsqlParameter("active", active.Value));
                if (!string.IsNullOrWhiteSpace(search))
                    filters.Add("name ilike @search", new NpgsqlParameter("search", $"%{search.Trim()}%"));
            });

            var sql = $"""
                select id, sport_id, country_id, name, active, short_code, image_path,
                       type, sub_type, category,
                       count(*) over() as total_count
                from competition.leagues
                {where}
                order by name
                limit @limit offset @offset;
                """;

            return await ReadPagedAsync(sql, parameters, page, perPage, MapLeague, ct);
        }

        public async Task<LeagueDto?> GetLeagueByIdAsync(long id, CancellationToken ct = default)
        {
            const string sql = """
                select id, sport_id, country_id, name, active, short_code, image_path,
                       type, sub_type, category
                from competition.leagues
                where id = @id
                limit 1;
                """;

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.Add(new NpgsqlParameter("id", id));

            await using var reader = await command.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
                return null;

            return MapLeague(reader);
        }

        public async Task<(IReadOnlyList<SeasonDto> Items, int Total)> GetSeasonsAsync(
            long? leagueId, bool? isCurrent, int page, int perPage, CancellationToken ct = default)
        {
            var (where, parameters) = BuildWhere(filters =>
            {
                if (leagueId.HasValue)
                    filters.Add("league_id = @league_id", new NpgsqlParameter("league_id", leagueId.Value));
                if (isCurrent.HasValue)
                    filters.Add("is_current = @is_current", new NpgsqlParameter("is_current", isCurrent.Value));
            });

            var sql = $"""
                select id, league_id, name, finished, pending, is_current, starting_at, ending_at,
                       count(*) over() as total_count
                from competition.seasons
                {where}
                order by starting_at desc nulls last, id desc
                limit @limit offset @offset;
                """;

            return await ReadPagedAsync(sql, parameters, page, perPage, MapSeason, ct);
        }

        public async Task<SeasonDto?> GetSeasonByIdAsync(long id, CancellationToken ct = default)
        {
            const string sql = """
                select id, league_id, name, finished, pending, is_current, starting_at, ending_at
                from competition.seasons
                where id = @id
                limit 1;
                """;

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.Add(new NpgsqlParameter("id", id));

            await using var reader = await command.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
                return null;

            return MapSeason(reader);
        }

        public async Task<(IReadOnlyList<TeamDto> Items, int Total)> GetTeamsAsync(
            long? countryId, string? search, int page, int perPage, CancellationToken ct = default)
        {
            var (where, parameters) = BuildWhere(filters =>
            {
                if (countryId.HasValue)
                    filters.Add("country_id = @country_id", new NpgsqlParameter("country_id", countryId.Value));
                if (!string.IsNullOrWhiteSpace(search))
                    filters.Add("name ilike @search", new NpgsqlParameter("search", $"%{search.Trim()}%"));
            });

            var sql = $"""
                select id, country_id, venue_id, name, short_code, image_path, founded, type, gender,
                       count(*) over() as total_count
                from football.teams
                {where}
                order by name
                limit @limit offset @offset;
                """;

            return await ReadPagedAsync(sql, parameters, page, perPage, MapTeam, ct);
        }

        public async Task<TeamDto?> GetTeamByIdAsync(long id, CancellationToken ct = default)
        {
            const string sql = """
                select id, country_id, venue_id, name, short_code, image_path, founded, type, gender
                from football.teams
                where id = @id
                limit 1;
                """;

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.Add(new NpgsqlParameter("id", id));

            await using var reader = await command.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
                return null;

            return MapTeam(reader);
        }

        public async Task<IReadOnlyList<TeamSeasonStatsDto>> GetTeamSeasonStatsAsync(
            long teamId, long? seasonId, CancellationToken ct = default)
        {
            // Most recent as_of_date per (league, season) — analytics worker
            // rewrites the same row daily, but historic seasons stick at
            // their final date. Filter to fixture_scope='all' for the
            // headline numbers; future filters (home/away splits) can
            // pull additional scopes off the same table.
            var seasonFilter = seasonId.HasValue ? "and season_id = @season_id" : "";
            var sql = $"""
                with ranked as (
                    select s.*,
                           row_number() over (
                               partition by league_id, season_id
                               order by as_of_date desc
                           ) as rn
                    from analytics.season_team_stats s
                    where team_id = @team_id
                      and fixture_scope = 'all'
                      {seasonFilter}
                )
                select league_id, season_id, team_id, as_of_date, fixture_scope,
                       matches_played, matches_won, matches_drawn, matches_lost,
                       goals_for, goals_against, goal_difference,
                       clean_sheets, failed_to_score, both_teams_scored,
                       yellow_cards, red_cards,
                       average_goals_for, average_goals_against,
                       points, form
                from ranked
                where rn = 1
                order by as_of_date desc, league_id;
                """;

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.Add(new NpgsqlParameter("team_id", teamId));
            if (seasonId.HasValue)
                command.Parameters.Add(new NpgsqlParameter("season_id", seasonId.Value));

            var items = new List<TeamSeasonStatsDto>();
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                items.Add(new TeamSeasonStatsDto
                {
                    LeagueId = reader.GetInt64(reader.GetOrdinal("league_id")),
                    SeasonId = reader.GetInt64(reader.GetOrdinal("season_id")),
                    TeamId = reader.GetInt64(reader.GetOrdinal("team_id")),
                    AsOfDate = reader.GetDateTime(reader.GetOrdinal("as_of_date")),
                    FixtureScope = reader.GetString(reader.GetOrdinal("fixture_scope")),
                    MatchesPlayed = ReadNullableInt(reader, "matches_played"),
                    MatchesWon = ReadNullableInt(reader, "matches_won"),
                    MatchesDrawn = ReadNullableInt(reader, "matches_drawn"),
                    MatchesLost = ReadNullableInt(reader, "matches_lost"),
                    GoalsFor = ReadNullableInt(reader, "goals_for"),
                    GoalsAgainst = ReadNullableInt(reader, "goals_against"),
                    GoalDifference = ReadNullableInt(reader, "goal_difference"),
                    CleanSheets = ReadNullableInt(reader, "clean_sheets"),
                    FailedToScore = ReadNullableInt(reader, "failed_to_score"),
                    BothTeamsScored = ReadNullableInt(reader, "both_teams_scored"),
                    YellowCards = ReadNullableInt(reader, "yellow_cards"),
                    RedCards = ReadNullableInt(reader, "red_cards"),
                    AverageGoalsFor = ReadNullableDecimal(reader, "average_goals_for"),
                    AverageGoalsAgainst = ReadNullableDecimal(reader, "average_goals_against"),
                    Points = ReadNullableInt(reader, "points"),
                    Form = ReadNullableString(reader, "form"),
                });
            }
            return items;
        }

        public async Task<IReadOnlyList<TeamSquadMemberDto>> GetTeamSquadAsync(
            long teamId, long? seasonId, CancellationToken ct = default)
        {
            // Latest squad season when caller didn't pin one. team_squads
            // rows persist across seasons; the front page wants the
            // newest roster. Inner-join players so missing rows on the
            // reference side are skipped (rare but happens when a
            // transfer fires before the player sync catches up).
            var seasonClause = seasonId.HasValue
                ? "and ts.season_id = @season_id"
                : """
                  and ts.season_id = (
                      select max(season_id) from football.team_squads
                      where team_id = @team_id
                  )
                  """;

            var sql = $"""
                select ts.player_id, ts.season_id, ts.jersey_number, ts.captain,
                       ts.position_id,
                       p.name, p.display_name, p.first_name, p.last_name,
                       p.image_path, p.date_of_birth, p.nationality_id,
                       p.height, p.weight,
                       pos.developer_name as position_code
                from football.team_squads ts
                join football.players p on p.id = ts.player_id
                left join catalog.types pos on pos.id = ts.position_id
                where ts.team_id = @team_id
                  {seasonClause}
                order by pos.developer_name nulls last, ts.jersey_number nulls last, p.name;
                """;

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.Add(new NpgsqlParameter("team_id", teamId));
            if (seasonId.HasValue)
                command.Parameters.Add(new NpgsqlParameter("season_id", seasonId.Value));

            var items = new List<TeamSquadMemberDto>();
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                items.Add(new TeamSquadMemberDto
                {
                    PlayerId = reader.GetInt64(reader.GetOrdinal("player_id")),
                    SeasonId = ReadNullableLong(reader, "season_id"),
                    Name = ReadNullableString(reader, "name") ?? string.Empty,
                    DisplayName = ReadNullableString(reader, "display_name"),
                    FirstName = ReadNullableString(reader, "first_name"),
                    LastName = ReadNullableString(reader, "last_name"),
                    ImagePath = ReadNullableString(reader, "image_path"),
                    DateOfBirth = ReadNullableDate(reader, "date_of_birth"),
                    NationalityId = ReadNullableLong(reader, "nationality_id"),
                    Height = ReadNullableInt(reader, "height"),
                    Weight = ReadNullableInt(reader, "weight"),
                    JerseyNumber = ReadNullableInt(reader, "jersey_number"),
                    Captain = ReadNullableBool(reader, "captain"),
                    PositionId = ReadNullableLong(reader, "position_id"),
                    PositionCode = ReadNullableString(reader, "position_code"),
                });
            }
            return items;
        }

        public async Task<(IReadOnlyList<BookmakerDto> Items, int Total)> GetBookmakersAsync(
            bool? active, int page, int perPage, CancellationToken ct = default)
        {
            var (where, parameters) = BuildWhere(filters =>
            {
                if (active.HasValue)
                    filters.Add("active = @active", new NpgsqlParameter("active", active.Value));
            });

            var sql = $"""
                select id, name, logo_path, active, available_in_standard, available_in_premium,
                       count(*) over() as total_count
                from odds.bookmakers
                {where}
                order by name
                limit @limit offset @offset;
                """;

            return await ReadPagedAsync(sql, parameters, page, perPage,
                r => new BookmakerDto
                {
                    Id = r.GetInt64(r.GetOrdinal("id")),
                    Name = r.GetString(r.GetOrdinal("name")),
                    LogoPath = ReadNullableString(r, "logo_path"),
                    Active = r.GetBoolean(r.GetOrdinal("active")),
                    AvailableInStandard = r.GetBoolean(r.GetOrdinal("available_in_standard")),
                    AvailableInPremium = r.GetBoolean(r.GetOrdinal("available_in_premium"))
                }, ct);
        }

        public async Task<(IReadOnlyList<MarketDto> Items, int Total)> GetMarketsAsync(
            bool? active, string? search, int page, int perPage, CancellationToken ct = default)
        {
            var (where, parameters) = BuildWhere(filters =>
            {
                if (active.HasValue)
                    filters.Add("active = @active", new NpgsqlParameter("active", active.Value));
                if (!string.IsNullOrWhiteSpace(search))
                    filters.Add("(name ilike @search or developer_name ilike @search)",
                        new NpgsqlParameter("search", $"%{search.Trim()}%"));
            });

            var sql = $"""
                select id, name, developer_name, has_winning_calculations, active,
                       available_in_standard, available_in_premium,
                       count(*) over() as total_count
                from odds.markets
                {where}
                order by name
                limit @limit offset @offset;
                """;

            return await ReadPagedAsync(sql, parameters, page, perPage,
                r => new MarketDto
                {
                    Id = r.GetInt64(r.GetOrdinal("id")),
                    Name = r.GetString(r.GetOrdinal("name")),
                    DeveloperName = ReadNullableString(r, "developer_name"),
                    HasWinningCalculations = ReadNullableBool(r, "has_winning_calculations"),
                    Active = r.GetBoolean(r.GetOrdinal("active")),
                    AvailableInStandard = r.GetBoolean(r.GetOrdinal("available_in_standard")),
                    AvailableInPremium = r.GetBoolean(r.GetOrdinal("available_in_premium"))
                }, ct);
        }

        private static LeagueDto MapLeague(NpgsqlDataReader r) => new()
        {
            Id = r.GetInt64(r.GetOrdinal("id")),
            SportId = r.GetInt64(r.GetOrdinal("sport_id")),
            CountryId = ReadNullableLong(r, "country_id"),
            Name = r.GetString(r.GetOrdinal("name")),
            Active = r.GetBoolean(r.GetOrdinal("active")),
            ShortCode = ReadNullableString(r, "short_code"),
            ImagePath = ReadNullableString(r, "image_path"),
            Type = ReadNullableString(r, "type"),
            SubType = ReadNullableString(r, "sub_type"),
            Category = ReadNullableInt(r, "category")
        };

        private static SeasonDto MapSeason(NpgsqlDataReader r) => new()
        {
            Id = r.GetInt64(r.GetOrdinal("id")),
            LeagueId = r.GetInt64(r.GetOrdinal("league_id")),
            Name = r.GetString(r.GetOrdinal("name")),
            Finished = r.GetBoolean(r.GetOrdinal("finished")),
            Pending = r.GetBoolean(r.GetOrdinal("pending")),
            IsCurrent = r.GetBoolean(r.GetOrdinal("is_current")),
            StartingAt = ReadNullableDateOnly(r, "starting_at"),
            EndingAt = ReadNullableDateOnly(r, "ending_at")
        };

        private static TeamDto MapTeam(NpgsqlDataReader r) => new()
        {
            Id = r.GetInt64(r.GetOrdinal("id")),
            CountryId = ReadNullableLong(r, "country_id"),
            VenueId = ReadNullableLong(r, "venue_id"),
            Name = r.GetString(r.GetOrdinal("name")),
            ShortCode = ReadNullableString(r, "short_code"),
            ImagePath = ReadNullableString(r, "image_path"),
            Founded = ReadNullableInt(r, "founded"),
            Type = ReadNullableString(r, "type"),
            Gender = ReadNullableString(r, "gender")
        };

        private async Task<(IReadOnlyList<T> Items, int Total)> ReadPagedAsync<T>(
            string sql,
            List<NpgsqlParameter> parameters,
            int page,
            int perPage,
            Func<NpgsqlDataReader, T> mapper,
            CancellationToken ct)
        {
            parameters.Add(new NpgsqlParameter("limit", perPage));
            parameters.Add(new NpgsqlParameter("offset", (page - 1) * perPage));

            await using var connection = await OpenAsync(ct);
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddRange(parameters.ToArray());

            var items = new List<T>();
            var total = 0;

            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                if (total == 0)
                    total = reader.GetInt32(reader.GetOrdinal("total_count"));
                items.Add(mapper(reader));
            }

            return (items, total);
        }

        private static (string Clause, List<NpgsqlParameter> Parameters) BuildWhere(
            Action<FilterBuilder> configure)
        {
            var fb = new FilterBuilder();
            configure(fb);
            return fb.Build();
        }

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

        private static DateTime? ReadNullableDateOnly(NpgsqlDataReader r, string column)
        {
            var i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? null : r.GetDateTime(i);
        }

        private static DateTime? ReadNullableDate(NpgsqlDataReader r, string column)
            => ReadNullableDateOnly(r, column);

        private static decimal? ReadNullableDecimal(NpgsqlDataReader r, string column)
        {
            var i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? null : r.GetDecimal(i);
        }

        private sealed class FilterBuilder
        {
            private readonly List<string> _clauses = new();
            private readonly List<NpgsqlParameter> _parameters = new();

            public void Add(string clause, NpgsqlParameter parameter)
            {
                _clauses.Add(clause);
                _parameters.Add(parameter);
            }

            public (string Clause, List<NpgsqlParameter> Parameters) Build()
            {
                if (_clauses.Count == 0)
                    return (string.Empty, _parameters);

                var sb = new StringBuilder("where ");
                for (var i = 0; i < _clauses.Count; i++)
                {
                    if (i > 0) sb.Append(" and ");
                    sb.Append(_clauses[i]);
                }
                return (sb.ToString(), _parameters);
            }
        }
    }
}
