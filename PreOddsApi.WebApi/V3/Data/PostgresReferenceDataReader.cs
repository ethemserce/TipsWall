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
        private readonly string? _connectionString;

        public PostgresReferenceDataReader(IConfiguration configuration)
        {
            _connectionString = Environment.GetEnvironmentVariable("PREODDS_POSTGRES_CONNECTION")
                ?? configuration.GetConnectionString("PreOddsApiPostgresDb");
        }

        public async Task<(IReadOnlyList<CountryDto> Items, int Total)> GetCountriesAsync(
            long? continentId, string? search, int page, int perPage, CancellationToken ct = default)
        {
            var (where, parameters) = BuildWhere(filters =>
            {
                if (continentId.HasValue)
                    filters.Add("continent_id = @continent_id", new NpgsqlParameter("continent_id", continentId.Value));
                if (!string.IsNullOrWhiteSpace(search))
                    filters.Add("name ilike @search", new NpgsqlParameter("search", $"%{search.Trim()}%"));
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

        private static DateTime? ReadNullableDateOnly(NpgsqlDataReader r, string column)
        {
            var i = r.GetOrdinal(column);
            return r.IsDBNull(i) ? null : r.GetDateTime(i);
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
