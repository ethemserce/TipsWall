using PreOddsApi.Core.Data.EntityFramework.Abstract;
using PreOddsApi.DataLayer;
using PreOddsApi.Entities.PreOddsEntities;
using PreOddsApi.Entities.SportMonks.Core.V3;
using PreOddsApi.Entities.SportMonks.Football.V3;
using PreOddsApi.ExternalApis.SportMonks;
using PreOddsApi.ExternalApis.SportMonks.Sync;

namespace SportMonks.Core.Worker.WorkerServices
{
    public class CoreWorkerService : BackgroundService
    {
        private readonly ILogger<CoreWorkerService> _logger;
        private readonly ISportMonksSyncRunner _syncRunner;
        private readonly IUpsertService<PreOddsApiDbContext> _upsertService;

        public CoreWorkerService(
            ILogger<CoreWorkerService> logger,
            ISportMonksSyncRunner syncRunner,
            IUpsertService<PreOddsApiDbContext> upsertService)
        {
            _logger = logger;
            _syncRunner = syncRunner;
            _upsertService = upsertService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Core Service execution started!");
            try
            {
                await ExecuteCoreData(stoppingToken);

                // TO DO: Core data insert to database
            }
            catch (Exception exc)
            {
                _logger.LogError(exc, exc.Message);
            }
        }

        private async Task ExecuteCoreData(CancellationToken cancellationToken)
        {
            var continents = (await _syncRunner.GetAllAsync<Continent>(
                SportMonksSyncJobDefinition.Create(
                    "sportmonks.core.continents",
                    "core.continent",
                    "Sync SportMonks continents with countries include."),
                SportMonksApiRequest.Create("continents")
                    .WithInclude("countries"),
                cancellationToken: cancellationToken)).ToList();

            var countries = (await _syncRunner.GetAllAsync<Country>(
                SportMonksSyncJobDefinition.Create(
                    "sportmonks.core.countries",
                    "core.country",
                    "Sync SportMonks countries with regions and cities include."),
                SportMonksApiRequest.Create("countries")
                    .WithInclude("regions.cities"),
                cancellationToken: cancellationToken)).ToList();

            foreach (var continent in continents)
            {
                if (continent.Countries == null)
                {
                    continue;
                }

                foreach (var country in continent.Countries)
                {
                    var matchedCountry = countries.FirstOrDefault(x => x.Id == country.Id);
                    if (matchedCountry?.Regions != null)
                    {
                        country.Regions.AddRange(matchedCountry.Regions);
                    }
                }
            }

            await _upsertService.UpsertAsync<Continent, continent>(continents);
        }

        private async Task ExecuteContinents(CancellationToken cancellationToken)
        {
            var continents = (await _syncRunner.GetAllAsync<Continent>(
                SportMonksSyncJobDefinition.Create(
                    "sportmonks.core.continents",
                    "core.continent",
                    "Sync SportMonks continents."),
                SportMonksApiRequest.Create("continents"),
                cancellationToken: cancellationToken)).ToList();

            await _upsertService.UpsertAsync<Continent, continent>(continents);
        }

        private async Task ExecuteCountries(CancellationToken cancellationToken)
        {
            var countries = (await _syncRunner.GetAllAsync<Country>(
                SportMonksSyncJobDefinition.Create(
                    "sportmonks.core.countries",
                    "core.country",
                    "Sync SportMonks countries."),
                SportMonksApiRequest.Create("countries"),
                cancellationToken: cancellationToken)).ToList();

            await _upsertService.UpsertAsync<Country, country>(countries);
        }

        private async Task ExecuteRegions(CancellationToken cancellationToken)
        {
            var regions = (await _syncRunner.GetAllAsync<Region>(
                SportMonksSyncJobDefinition.Create(
                    "sportmonks.core.regions",
                    "core.region",
                    "Sync SportMonks regions."),
                SportMonksApiRequest.Create("regions"),
                cancellationToken: cancellationToken)).ToList();

            await _upsertService.UpsertAsync<Region, region>(regions);
        }

        private async Task ExecuteCities(CancellationToken cancellationToken)
        {
            var cities = (await _syncRunner.GetAllAsync<City>(
                SportMonksSyncJobDefinition.Create(
                    "sportmonks.core.cities",
                    "core.city",
                    "Sync SportMonks cities."),
                SportMonksApiRequest.Create("cities"),
                cancellationToken: cancellationToken)).ToList();

            await _upsertService.UpsertAsync<City, city>(cities);
        }

        private async Task ExecuteTypes(CancellationToken cancellationToken)
        {
            var typeList = (await _syncRunner.GetAllAsync<Types>(
                SportMonksSyncJobDefinition.Create(
                    "sportmonks.core.types",
                    "core.type",
                    "Sync SportMonks types."),
                SportMonksApiRequest.Create("types"),
                cancellationToken: cancellationToken)).ToList();

            await _upsertService.UpsertAsync<Types, types>(typeList);
        }

        private async Task ExecuteStates(CancellationToken cancellationToken)
        {
            var states = (await _syncRunner.GetAllAsync<State>(
                SportMonksSyncJobDefinition.Create(
                    "sportmonks.football.states",
                    "football.state",
                    "Sync SportMonks football states."),
                SportMonksApiRequest.Create("states"),
                cancellationToken: cancellationToken)).ToList();

            await _upsertService.UpsertAsync<State, state>(states);
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Core Service started!");
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Core Service stppped!");
            return base.StopAsync(cancellationToken);
        }
    }
}
