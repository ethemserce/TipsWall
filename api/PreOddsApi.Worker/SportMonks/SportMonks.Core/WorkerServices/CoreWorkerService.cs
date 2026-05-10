using System.Globalization;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using PreOddsApi.Entities.SportMonks.Core.V3;
using PreOddsApi.ExternalApis.SportMonks;
using PreOddsApi.ExternalApis.SportMonks.Sync;
using PreOddsApi.ExternalApis.SportMonks.Sync.Writers;

namespace SportMonks.Core.Worker.WorkerServices
{
    public class CoreWorkerService : BackgroundService
    {
        private const string CoreReferenceKey = "worker.core.reference";
        private const string UsageKey = "worker.core.usage";

        private readonly ILogger<CoreWorkerService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ISyncJobScheduler _scheduler;
        private readonly ISportMonksSyncRunner _syncRunner;
        private readonly ISportMonksApiClient _apiClient;
        private readonly ISportMonksCatalogReferenceWriter _catalogReferenceWriter;

        public CoreWorkerService(
            ILogger<CoreWorkerService> logger,
            IConfiguration configuration,
            ISyncJobScheduler scheduler,
            ISportMonksSyncRunner syncRunner,
            ISportMonksApiClient apiClient,
            ISportMonksCatalogReferenceWriter catalogReferenceWriter)
        {
            _logger = logger;
            _configuration = configuration;
            _scheduler = scheduler;
            _syncRunner = syncRunner;
            _apiClient = apiClient;
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
                    await MaybeRunSubscriptionUsageAsync(stoppingToken);
                    await MaybeRunCoreReferenceAsync(stoppingToken);
                }
                catch (Exception exc)
                {
                    _logger.LogError(exc, exc.Message);
                }

                await Task.Delay(pollingInterval, stoppingToken);
            }
        }

        private async Task MaybeRunSubscriptionUsageAsync(CancellationToken cancellationToken)
        {
            var interval = GetInteger("SportMonksCoreWorkerSettings:UsageIntervalSeconds", 86400);
            if (!_scheduler.ShouldRun(UsageKey, interval))
                return;

            try
            {
                // `/v3/my/usage` lives outside the per-sport namespace (no
                // `core/` or `football/` prefix). The URL builder passes through
                // any endpoint that already starts with "v3/".
                var request = SportMonksApiRequest.Create("v3/my/usage")
                    .WithoutDefaultPagination();
                var raw = await _apiClient.GetRawAsync(request, cancellationToken);
                var json = JObject.Parse(raw);

                var planNames = (json["subscription"] as JArray ?? new JArray())
                    .SelectMany(sub => sub["plans"] as JArray ?? new JArray())
                    .Select(plan => plan["plan"]?.Value<string>())
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .ToList();
                var addOns = (json["subscription"] as JArray ?? new JArray())
                    .SelectMany(sub => sub["add_ons"] as JArray ?? new JArray())
                    .Select(addon => addon["name"]?.Value<string>() ?? addon.ToString())
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .ToList();
                var remaining = json["rate_limit"]?["remaining"]?.Value<int?>();
                var resetsIn = json["rate_limit"]?["resets_in_seconds"]?.Value<int?>();

                _logger.LogInformation(
                    "SportMonks subscription: plans=[{Plans}] add_ons=[{AddOns}] rate_limit_remaining={Remaining} resets_in={ResetsIn}s",
                    planNames.Count == 0 ? "(none)" : string.Join(" | ", planNames),
                    addOns.Count == 0 ? "(none)" : string.Join(" | ", addOns),
                    remaining,
                    resetsIn);

                if (remaining is < 600)
                {
                    _logger.LogWarning(
                        "SportMonks rate limit running low: {Remaining} requests remain until reset in {ResetsIn}s.",
                        remaining,
                        resetsIn);
                }

                _scheduler.RecordRun(UsageKey);
            }
            catch (Exception exc)
            {
                // Telemetry must not block the rest of the worker; log and move on.
                _logger.LogWarning(
                    exc,
                    "SportMonks subscription usage probe failed: {Message}",
                    exc.Message);
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
