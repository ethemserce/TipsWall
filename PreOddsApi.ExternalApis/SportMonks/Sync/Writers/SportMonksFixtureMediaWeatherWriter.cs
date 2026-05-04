using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;
using PreOddsApi.Entities.SportMonks.Core.V3;
using PreOddsApi.Entities.SportMonks.Football;
using PreOddsApi.Entities.SportMonks.Football.V3;
using PreOddsApi.Entities.SportMonks.Football.Weather.V3;

namespace PreOddsApi.ExternalApis.SportMonks.Sync.Writers
{
    public sealed class SportMonksFixtureMediaWeatherWriter : ISportMonksFixtureMediaWeatherWriter
    {
        private readonly string? _connectionString;
        private readonly ILogger<SportMonksFixtureMediaWeatherWriter> _logger;

        public SportMonksFixtureMediaWeatherWriter(
            IConfiguration configuration,
            ILogger<SportMonksFixtureMediaWeatherWriter> logger)
        {
            _connectionString = Environment.GetEnvironmentVariable("PREODDS_POSTGRES_CONNECTION")
                ?? configuration.GetConnectionString("PreOddsApiPostgresDb");
            _logger = logger;
        }

        public async Task UpsertTvStationsAsync(
            IEnumerable<TvStation> tvStations,
            CancellationToken cancellationToken = default)
        {
            var tvStationList = tvStations
                .Where(tvStation => tvStation != null && tvStation.Id > 0)
                .GroupBy(tvStation => tvStation.Id)
                .Select(group => group.Last())
                .ToList();

            if (tvStationList.Count == 0)
            {
                return;
            }

            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                var countryLinkCount = 0;

                foreach (var tvStation in tvStationList)
                {
                    await UpsertTvStationAsync(connection, transaction, tvStation, cancellationToken);

                    foreach (var country in tvStation.Countries ?? Enumerable.Empty<Country>())
                    {
                        if (await UpsertTvStationCountryAsync(
                                connection,
                                transaction,
                                tvStation.Id,
                                country,
                                cancellationToken))
                        {
                            countryLinkCount++;
                        }
                    }
                }

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "Upserted {TvStationCount} TV stations and {CountryLinkCount} TV station country links.",
                    tvStationList.Count,
                    countryLinkCount);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task UpsertFixtureMediaWeatherAsync(
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
                var tvStationCount = 0;
                var fixtureTvStationCount = 0;
                var countryLinkCount = 0;
                var weatherReportCount = 0;

                foreach (var fixture in fixtureList)
                {
                    foreach (var tvStation in fixture.TvStations ?? Enumerable.Empty<TvStation>())
                    {
                        if (tvStation == null || tvStation.Id == 0)
                        {
                            continue;
                        }

                        await UpsertTvStationAsync(connection, transaction, tvStation, cancellationToken);
                        tvStationCount++;

                        foreach (var country in tvStation.Countries ?? Enumerable.Empty<Country>())
                        {
                            if (await UpsertTvStationCountryAsync(
                                    connection,
                                    transaction,
                                    tvStation.Id,
                                    country,
                                    cancellationToken))
                            {
                                countryLinkCount++;
                            }
                        }

                        if (await UpsertFixtureTvStationAsync(
                                connection,
                                transaction,
                                fixture.Id,
                                tvStation.Id,
                                cancellationToken))
                        {
                            fixtureTvStationCount++;
                        }
                    }

                    if (fixture.WeatherReport != null &&
                        await UpsertWeatherReportAsync(
                            connection,
                            transaction,
                            fixture.Id,
                            fixture.WeatherReport,
                            cancellationToken))
                    {
                        weatherReportCount++;
                    }
                }

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "Upserted {TvStationCount} fixture TV station rows, {FixtureTvStationCount} fixture TV station links, {CountryLinkCount} country links, and {WeatherReportCount} weather reports.",
                    tvStationCount,
                    fixtureTvStationCount,
                    countryLinkCount,
                    weatherReportCount);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private static async Task UpsertTvStationAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            TvStation tvStation,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into football.tv_stations (
                    id,
                    name,
                    url,
                    image_path,
                    last_synced_at)
                values (
                    @id,
                    @name,
                    @url,
                    @image_path,
                    now())
                on conflict (id) do update set
                    name = excluded.name,
                    url = coalesce(excluded.url, football.tv_stations.url),
                    image_path = coalesce(excluded.image_path, football.tv_stations.image_path),
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("id", tvStation.Id));
            command.Parameters.Add(Parameter("name", GetRequiredName(tvStation.Name, "tv-station", tvStation.Id)));
            command.Parameters.Add(TextParameter("url", NullIfWhiteSpace(tvStation.Url)));
            command.Parameters.Add(TextParameter("image_path", NullIfWhiteSpace(tvStation.ImagePath)));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task<bool> UpsertTvStationCountryAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            long tvStationId,
            Country country,
            CancellationToken cancellationToken)
        {
            if (tvStationId == 0 || country == null || country.Id == 0)
            {
                return false;
            }

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into football.tv_station_countries (
                    tv_station_id,
                    country_id)
                select
                    tv_stations.id,
                    countries.id
                from football.tv_stations tv_stations
                cross join catalog.countries countries
                where tv_stations.id = @tv_station_id
                  and countries.id = @country_id
                on conflict (tv_station_id, country_id) do nothing;
                """;
            command.Parameters.Add(Parameter("tv_station_id", tvStationId));
            command.Parameters.Add(Parameter("country_id", country.Id));

            return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
        }

        private static async Task<bool> UpsertFixtureTvStationAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            long fixtureId,
            long tvStationId,
            CancellationToken cancellationToken)
        {
            if (fixtureId == 0 || tvStationId == 0)
            {
                return false;
            }

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into football.fixture_tv_stations (
                    fixture_id,
                    tv_station_id)
                select
                    fixtures.id,
                    tv_stations.id
                from football.fixtures fixtures
                cross join football.tv_stations tv_stations
                where fixtures.id = @fixture_id
                  and tv_stations.id = @tv_station_id
                on conflict (fixture_id, tv_station_id) do nothing;
                """;
            command.Parameters.Add(Parameter("fixture_id", fixtureId));
            command.Parameters.Add(Parameter("tv_station_id", tvStationId));

            return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
        }

        private static async Task<bool> UpsertWeatherReportAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            long fallbackFixtureId,
            WeatherReport weatherReport,
            CancellationToken cancellationToken)
        {
            var fixtureId = ResolveId(weatherReport.FixtureId, weatherReport.Fixture?.Id, fallbackFixtureId);
            if (fixtureId == 0)
            {
                return false;
            }

            var weatherReportId = weatherReport.Id > 0
                ? weatherReport.Id
                : GenerateSyntheticWeatherReportId(fixtureId);
            var venueId = ResolveId(weatherReport.VenueId, weatherReport.Venue?.Id);

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into football.fixture_weather_reports (
                    id,
                    fixture_id,
                    venue_id,
                    temperature,
                    feels_like,
                    wind,
                    humidity,
                    pressure,
                    clouds,
                    description,
                    icon,
                    type,
                    metric,
                    current,
                    last_synced_at)
                select
                    @id,
                    fixtures.id,
                    (select id from football.venues where id = @venue_id),
                    @temperature,
                    @feels_like,
                    @wind,
                    @humidity,
                    @pressure,
                    @clouds,
                    @description,
                    @icon,
                    @type,
                    @metric,
                    @current,
                    now()
                from football.fixtures fixtures
                where fixtures.id = @fixture_id
                on conflict (id) do update set
                    fixture_id = excluded.fixture_id,
                    venue_id = coalesce(excluded.venue_id, football.fixture_weather_reports.venue_id),
                    temperature = coalesce(excluded.temperature, football.fixture_weather_reports.temperature),
                    feels_like = coalesce(excluded.feels_like, football.fixture_weather_reports.feels_like),
                    wind = coalesce(excluded.wind, football.fixture_weather_reports.wind),
                    humidity = coalesce(excluded.humidity, football.fixture_weather_reports.humidity),
                    pressure = coalesce(excluded.pressure, football.fixture_weather_reports.pressure),
                    clouds = coalesce(excluded.clouds, football.fixture_weather_reports.clouds),
                    description = coalesce(excluded.description, football.fixture_weather_reports.description),
                    icon = coalesce(excluded.icon, football.fixture_weather_reports.icon),
                    type = coalesce(excluded.type, football.fixture_weather_reports.type),
                    metric = coalesce(excluded.metric, football.fixture_weather_reports.metric),
                    current = coalesce(excluded.current, football.fixture_weather_reports.current),
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("id", weatherReportId));
            command.Parameters.Add(Parameter("fixture_id", fixtureId));
            command.Parameters.Add(BigIntParameter("venue_id", NullIfZero(venueId)));
            command.Parameters.Add(JsonbParameter("temperature", weatherReport.Temperature));
            command.Parameters.Add(JsonbParameter("feels_like", weatherReport.FeelsLike));
            command.Parameters.Add(JsonbParameter("wind", weatherReport.Wind));
            command.Parameters.Add(TextParameter("humidity", NullIfWhiteSpace(weatherReport.Humidity)));
            command.Parameters.Add(IntegerParameter("pressure", NullIfZero(weatherReport.Pressure)));
            command.Parameters.Add(TextParameter("clouds", NullIfWhiteSpace(weatherReport.Clouds)));
            command.Parameters.Add(TextParameter("description", NullIfWhiteSpace(weatherReport.Description)));
            command.Parameters.Add(TextParameter("icon", NullIfWhiteSpace(weatherReport.Icon)));
            command.Parameters.Add(TextParameter("type", NullIfWhiteSpace(weatherReport.Type)));
            command.Parameters.Add(TextParameter("metric", NullIfWhiteSpace(weatherReport.Metric)));
            command.Parameters.Add(TextParameter("current", ScalarToString(weatherReport.Current)));

            return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
        }

        private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                throw new InvalidOperationException(
                    "PostgreSQL connection string 'PreOddsApiPostgresDb' is required for fixture media/weather sync.");
            }

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }

        private static long GenerateSyntheticWeatherReportId(long fixtureId)
        {
            return -Math.Max(1, fixtureId);
        }

        private static long ResolveId(params long?[] values)
        {
            return values.FirstOrDefault(value => value.GetValueOrDefault() > 0).GetValueOrDefault();
        }

        private static string GetRequiredName(string? value, string entityName, long id)
        {
            return string.IsNullOrWhiteSpace(value)
                ? $"{entityName}-{id}"
                : value.Trim();
        }

        private static string? ScalarToString(object? value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is bool boolean)
            {
                return boolean ? "true" : "false";
            }

            if (value is IFormattable formattable)
            {
                return NullIfWhiteSpace(formattable.ToString(null, CultureInfo.InvariantCulture));
            }

            return NullIfWhiteSpace(value.ToString());
        }

        private static string? NullIfWhiteSpace(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static long? NullIfZero(long value)
        {
            return value == 0 ? null : value;
        }

        private static int? NullIfZero(int? value)
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
