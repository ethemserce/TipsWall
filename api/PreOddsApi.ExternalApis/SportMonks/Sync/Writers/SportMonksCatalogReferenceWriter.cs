using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using PreOddsApi.Entities.SportMonks.Core.V3;

namespace PreOddsApi.ExternalApis.SportMonks.Sync.Writers
{
    public sealed class SportMonksCatalogReferenceWriter : ISportMonksCatalogReferenceWriter
    {
        private readonly string? _connectionString;
        private readonly ILogger<SportMonksCatalogReferenceWriter> _logger;

        public SportMonksCatalogReferenceWriter(
            IConfiguration configuration,
            ILogger<SportMonksCatalogReferenceWriter> logger)
        {
            _connectionString = configuration.GetConnectionString("PreOddsApiPostgresDb");
            _logger = logger;
        }

        public async Task UpsertContinentsAsync(
            IEnumerable<Continent> continents,
            CancellationToken cancellationToken = default)
        {
            var continentList = continents
                .Where(continent => continent != null)
                .GroupBy(continent => continent.Id)
                .Select(group => group.Last())
                .ToList();

            if (continentList.Count == 0)
            {
                return;
            }

            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                foreach (var continent in continentList)
                {
                    await UpsertContinentAsync(connection, transaction, continent, cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "Upserted {ContinentCount} SportMonks continents into catalog.continents.",
                    continentList.Count);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task UpsertCountriesWithRegionsAndCitiesAsync(
            IEnumerable<Country> countries,
            CancellationToken cancellationToken = default)
        {
            var countryList = countries
                .Where(country => country != null)
                .GroupBy(country => country.Id)
                .Select(group => group.Last())
                .ToList();

            if (countryList.Count == 0)
            {
                return;
            }

            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                var regionCount = 0;
                var cityCount = 0;

                foreach (var country in countryList)
                {
                    var localCountryId = await UpsertCountryAsync(connection, transaction, country, cancellationToken);

                    foreach (var region in country.Regions ?? Enumerable.Empty<Region>())
                    {
                        var localRegionId = await UpsertRegionAsync(connection, transaction, region, localCountryId, cancellationToken);
                        regionCount++;

                        foreach (var city in region.Cities ?? Enumerable.Empty<City>())
                        {
                            await UpsertCityAsync(
                                connection,
                                transaction,
                                city,
                                //country.Id,
                                localCountryId,
                                //region.Id,
                                localRegionId,
                                cancellationToken);
                            cityCount++;
                        }
                    }
                }

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "Upserted {CountryCount} countries, {RegionCount} regions, and {CityCount} cities into catalog schema.",
                    countryList.Count,
                    regionCount,
                    cityCount);
            }
            catch (Exception exc)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task UpsertTypesAsync(
            IEnumerable<Types> types,
            CancellationToken cancellationToken = default)
        {
            var typeList = types
                .Where(type => type != null)
                .GroupBy(type => type.Id)
                .Select(group => group.Last())
                .ToList();

            if (typeList.Count == 0)
            {
                return;
            }

            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                foreach (var type in typeList)
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
                            code = excluded.code,
                            developer_name = excluded.developer_name,
                            model_type = excluded.model_type,
                            stat_group = excluded.stat_group,
                            last_synced_at = now(),
                            updated_at = now();
                        """;
                    command.Parameters.Add(Parameter("id", type.Id));
                    command.Parameters.Add(Parameter("name", GetRequiredName(type.Name, "type", type.Id)));
                    command.Parameters.Add(Parameter("code", NullIfWhiteSpace(type.Code)));
                    command.Parameters.Add(Parameter("developer_name", NullIfWhiteSpace(type.DeveloperName)));
                    command.Parameters.Add(Parameter("model_type", NullIfWhiteSpace(type.ModelType)));
                    command.Parameters.Add(Parameter("stat_group", NullIfWhiteSpace(type.StatGroup)));

                    await command.ExecuteNonQueryAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "Upserted {TypeCount} SportMonks types into catalog.types.",
                    typeList.Count);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task UpsertSportsAsync(
            IEnumerable<Sport> sports,
            CancellationToken cancellationToken = default)
        {
            var sportList = sports
                .Where(sport => sport != null)
                .GroupBy(sport => sport.Id)
                .Select(group => group.Last())
                .ToList();

            if (sportList.Count == 0)
            {
                return;
            }

            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                foreach (var sport in sportList)
                {
                    await using var command = connection.CreateCommand();
                    command.Transaction = transaction;
                    command.CommandText = """
                        insert into catalog.sports (
                            id,
                            name,
                            code,
                            last_synced_at)
                        values (
                            @id,
                            @name,
                            @code,
                            now())
                        on conflict (id) do update set
                            name = excluded.name,
                            code = excluded.code,
                            last_synced_at = now(),
                            updated_at = now();
                        """;
                    command.Parameters.Add(Parameter("id", sport.Id));
                    command.Parameters.Add(Parameter("name", GetRequiredName(sport.Name, "sport", sport.Id)));
                    command.Parameters.Add(Parameter("code", NullIfWhiteSpace(sport.Code)));

                    await command.ExecuteNonQueryAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "Upserted {SportCount} SportMonks sports into catalog.sports.",
                    sportList.Count);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private static async Task UpsertContinentAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Continent continent,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into catalog.continents (
                    id,
                    name,
                    code,
                    last_synced_at)
                values (
                    @id,
                    @name,
                    @code,
                    now())
                on conflict (id) do update set
                    name = excluded.name,
                    code = excluded.code,
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("id", continent.Id));
            command.Parameters.Add(Parameter("name", GetRequiredName(continent.Name, "continent", continent.Id)));
            command.Parameters.Add(Parameter("code", NullIfWhiteSpace(continent.Code)));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task<long> UpsertCountryAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Country country,
            CancellationToken cancellationToken)
        {
            if (country.ContinentId == 0)
            {
                throw new InvalidOperationException(
                    $"SportMonks country {country.Id} cannot be written without a continent_id.");
            }

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into catalog.countries (
                    id,
                    continent_id,
                    name,
                    official_name,
                    fifa_name,
                    iso2,
                    iso3,
                    latitude,
                    longitude,
                    image_path,
                    borders,
                    last_synced_at)
                values (
                    @id,
                    @continent_id,
                    @name,
                    @official_name,
                    @fifa_name,
                    @iso2,
                    @iso3,
                    @latitude,
                    @longitude,
                    @image_path,
                    @borders,
                    now())
                on conflict (id) do update set
                    continent_id = excluded.continent_id,
                    name = excluded.name,
                    official_name = excluded.official_name,
                    fifa_name = excluded.fifa_name,
                    iso2 = excluded.iso2,
                    iso3 = excluded.iso3,
                    latitude = excluded.latitude,
                    longitude = excluded.longitude,
                    image_path = excluded.image_path,
                    borders = excluded.borders,
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("id", country.Id));
            command.Parameters.Add(Parameter("continent_id", country.ContinentId));
            command.Parameters.Add(Parameter("name", GetRequiredName(country.Name, "country", country.Id)));
            command.Parameters.Add(Parameter("official_name", NullIfWhiteSpace(country.OfficialName)));
            command.Parameters.Add(Parameter("fifa_name", NullIfWhiteSpace(country.FifaName)));
            command.Parameters.Add(Parameter("iso2", NullIfWhiteSpace(country.Iso2)));
            command.Parameters.Add(Parameter("iso3", NullIfWhiteSpace(country.Iso3)));
            command.Parameters.Add(Parameter("latitude", TryParseDecimal(country.Latitude)));
            command.Parameters.Add(Parameter("longitude", TryParseDecimal(country.Longitude)));
            command.Parameters.Add(Parameter("image_path", NullIfWhiteSpace(country.ImagePath)));
            command.Parameters.Add(new NpgsqlParameter("borders", NpgsqlDbType.Array | NpgsqlDbType.Text)
            {
                Value = DBNull.Value
            });

            await command.ExecuteNonQueryAsync(cancellationToken);
            return country.Id;
        }

        private static async Task<long> UpsertRegionAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Region region,
            long fallbackCountryId,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into catalog.regions (
                    id,
                    country_id,
                    name,
                    last_synced_at)
                values (
                    @id,
                    @country_id,
                    @name,
                    now())
                on conflict (id) do update set
                    country_id = excluded.country_id,
                    name = excluded.name,
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("id", region.Id));
            command.Parameters.Add(Parameter("country_id", fallbackCountryId));
            command.Parameters.Add(Parameter("name", GetRequiredName(region.Name, "region", region.Id)));

            await command.ExecuteNonQueryAsync(cancellationToken);
            return region.Id;
        }

        private static async Task<int> UpsertCityAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            City city,
            long fallbackCountryId,
            long fallbackRegionId,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into catalog.cities (
                    id,
                    country_id,
                    region_id,
                    name,
                    latitude,
                    longitude,
                    last_synced_at)
                values (
                    @id,
                    @country_id,
                    @region_id,
                    @name,
                    @latitude,
                    @longitude,
                    now())
                on conflict (id) do update set
                    country_id = excluded.country_id,
                    region_id = excluded.region_id,
                    name = excluded.name,
                    latitude = excluded.latitude,
                    longitude = excluded.longitude,
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("id", city.Id));
            var countryId = city.CountryId.GetValueOrDefault();
            if (countryId == 0)
            {
                countryId = fallbackCountryId;
            }

            var regionId = city.RegionId.GetValueOrDefault();
            if (regionId == 0)
            {
                regionId = fallbackRegionId;
            }

            command.Parameters.Add(Parameter("country_id", fallbackCountryId));
            command.Parameters.Add(Parameter("region_id", fallbackRegionId));
            command.Parameters.Add(Parameter("name", GetRequiredName(city.Name, "city", city.Id)));
            command.Parameters.Add(Parameter("latitude", TryParseDecimal(city.Latitude)));
            command.Parameters.Add(Parameter("longitude", TryParseDecimal(city.Longitude)));

            return await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                throw new InvalidOperationException(
                    "PostgreSQL connection string 'PreOddsApiPostgresDb' is required for catalog reference sync.");
            }

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }

        private static string GetRequiredName(string? value, string entityName, long id)
        {
            return string.IsNullOrWhiteSpace(value)
                ? $"{entityName}-{id}"
                : value.Trim();
        }

        private static string? NullIfWhiteSpace(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static decimal? TryParseDecimal(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result)
                ? result
                : null;
        }

        private static NpgsqlParameter Parameter(string name, object? value)
        {
            return new NpgsqlParameter(name, value ?? DBNull.Value);
        }
    }
}
