using System.Globalization;
using Microsoft.Extensions.Configuration;
using PreOddsApi.Entities.SportMonks.Core.V3;
using PreOddsApi.ExternalApis.SportMonks;
using PreOddsApi.ExternalApis.SportMonks.Sync;
using PreOddsApi.ExternalApis.SportMonks.Sync.Writers;

namespace SportMonks.Core.Worker.WorkerServices
{
    public class CoreWorkerService : BackgroundService
    {
        private const string CoreReferenceKey = "worker.core.reference";

        private readonly ILogger<CoreWorkerService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ISyncJobScheduler _scheduler;
        private readonly ISportMonksSyncRunner _syncRunner;
        private readonly ISportMonksCatalogReferenceWriter _catalogReferenceWriter;

        public CoreWorkerService(
            ILogger<CoreWorkerService> logger,
            IConfiguration configuration,
            ISyncJobScheduler scheduler,
            ISportMonksSyncRunner syncRunner,
            ISportMonksCatalogReferenceWriter catalogReferenceWriter)
        {
            _logger = logger;
            _configuration = configuration;
            _scheduler = scheduler;
            _syncRunner = syncRunner;
            _catalogReferenceWriter = catalogReferenceWriter;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var pollingInterval = TimeSpan.FromSeconds(
                GetInteger("SportMonksCoreWorkerSettings:PollingIntervalSeconds", 120));

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Core worker polling at {Time}.", DateTimeOffset.UtcNow);

                try
                {
                    await MaybeRunCoreReferenceAsync(stoppingToken);
                }
                catch (Exception exc)
                {
                    _logger.LogError(exc, exc.Message);
                }

                await Task.Delay(pollingInterval, stoppingToken);
            }
        }

        private async Task MaybeRunCoreReferenceAsync(CancellationToken cancellationToken)
        {
            var interval = GetInteger("SportMonksCoreWorkerSettings:CoreReferenceIntervalSeconds", 86400);
            if (!_scheduler.ShouldRun(CoreReferenceKey, interval))
                return;

            await ExecuteCoreData(cancellationToken);
            _scheduler.RecordRun(CoreReferenceKey);
        }

        private async Task ExecuteCoreData(CancellationToken cancellationToken)
        {
            var continents = await ExecuteContinents(cancellationToken);
            var countries = await ExecuteCountries(cancellationToken);
            var typeList = await ExecuteTypes(cancellationToken);

            ApplyMissingContinentIds(continents, countries);

            await _catalogReferenceWriter.UpsertContinentsAsync(continents, cancellationToken);
            await _catalogReferenceWriter.UpsertCountriesWithRegionsAndCitiesAsync(countries, cancellationToken);
            await _catalogReferenceWriter.UpsertTypesAsync(typeList, cancellationToken);
        }

        private async Task<List<Continent>> ExecuteContinents(CancellationToken cancellationToken)
        {
            return (await _syncRunner.GetAllAsync<Continent>(
                SportMonksSyncJobDefinition.Create(
                    "sportmonks.core.continents",
                    "core.continent",
                    "Sync SportMonks continents with countries include."),
                SportMonksApiRequest.Create("continents")
                    .WithInclude("countries"),
                cancellationToken: cancellationToken)).ToList();
        }

        private async Task<List<Country>> ExecuteCountries(CancellationToken cancellationToken)
        {
            return (await _syncRunner.GetAllAsync<Country>(
                SportMonksSyncJobDefinition.Create(
                    "sportmonks.core.countries",
                    "core.country",
                    "Sync SportMonks countries with regions and cities include."),
                SportMonksApiRequest.Create("countries")
                    .WithInclude("regions.cities"),
                cancellationToken: cancellationToken)).ToList();
        }

        private async Task<List<Types>> ExecuteTypes(CancellationToken cancellationToken)
        {
            return (await _syncRunner.GetAllAsync<Types>(
                SportMonksSyncJobDefinition.Create(
                    "sportmonks.core.types",
                    "core.type",
                    "Sync SportMonks types."),
                SportMonksApiRequest.Create("types"),
                cancellationToken: cancellationToken)).ToList();
        }

private static void ApplyMissingContinentIds(
            IEnumerable<Continent> continents,
            IEnumerable<Country> countries)
        {
            var continentIdByCountryId = continents
                .Where(continent => continent.Countries != null)
                .SelectMany(continent => continent.Countries.Select(country => new
                {
                    CountryId = country.Id,
                    ContinentId = continent.Id
                }))
                .GroupBy(x => x.CountryId)
                .ToDictionary(group => group.Key, group => group.Last().ContinentId);

            foreach (var country in countries)
            {
                if (country.ContinentId == 0
                    && continentIdByCountryId.TryGetValue(country.Id, out var continentId))
                {
                    country.ContinentId = continentId;
                }
            }
        }

        private int GetInteger(string key, int defaultValue)
        {
            return int.TryParse(
                _configuration[key],
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var result)
                ? result
                : defaultValue;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Core worker started.");
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Core worker stopped.");
            return base.StopAsync(cancellationToken);
        }
    }
}
