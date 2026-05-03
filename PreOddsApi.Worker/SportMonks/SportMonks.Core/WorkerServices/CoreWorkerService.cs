using Newtonsoft.Json;
using PreOddsApi.Core.Data.EntityFramework.Abstract;
using PreOddsApi.DataLayer;
using PreOddsApi.Entities.PreOddsEntities;
using PreOddsApi.Entities.SportMonks.Core.V3;
using PreOddsApi.Entities.SportMonks.Football.V3;
using PreOddsApi.ExternalApis.SportMonks;

namespace SportMonks.Core.Worker.WorkerServices
{
    public class CoreWorkerService : BackgroundService
    {
        private readonly ILogger<CoreWorkerService> _logger;
        private readonly ISportMonksApiClient _sportMonksApiClient;
        private readonly IUpsertService<PreOddsApiDbContext> _upsertService;

        public CoreWorkerService(ILogger<CoreWorkerService> logger,
                ISportMonksApiClient sportMonksApiClient,
                IUpsertService<PreOddsApiDbContext> upsertService)
        {
            _logger = logger;
            _sportMonksApiClient = sportMonksApiClient;
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

            //while (!stoppingToken.IsCancellationRequested)
            //{

            //    await Task.Delay(1000, stoppingToken);
            //}
        }

        private async Task ExecuteCoreData(CancellationToken cancellationToken)
        {
            var continents = (await _sportMonksApiClient.GetAllAsync<Continent>(
                SportMonksApiRequest.Create("continents")
                    .WithInclude("countries"),
                cancellationToken)).ToList();

            var countries = (await _sportMonksApiClient.GetAllAsync<Country>(
                SportMonksApiRequest.Create("countries")
                    .WithInclude("regions.cities"),
                cancellationToken)).ToList();

            foreach (var continent in continents)
            {
                foreach (var country in continent.Countries)
                {
                    var matchedCountry = countries.FirstOrDefault(x => x.Id == country.Id);
                    if (matchedCountry?.Regions != null)
                    {
                        country.Regions.AddRange(matchedCountry.Regions);
                    }
                }
            }

            var jsonData = JsonConvert.SerializeObject(continents);

            await _upsertService.UpsertAsync<Continent, continent>(continents);
        }

        private async Task ExecuteContinents(CancellationToken cancellationToken)
        {
            var continents = (await _sportMonksApiClient.GetAllAsync<Continent>(
                SportMonksApiRequest.Create("continents"),
                cancellationToken)).ToList();

            await _upsertService.UpsertAsync<Continent, continent>(continents);
        }

        private async Task ExecuteCountries(CancellationToken cancellationToken)
        {
            var countries = (await _sportMonksApiClient.GetAllAsync<Country>(
                SportMonksApiRequest.Create("countries"),
                cancellationToken)).ToList();

            await _upsertService.UpsertAsync<Country, country>(countries);
        }

        private async Task ExecuteRegions(CancellationToken cancellationToken)
        {
            var regions = (await _sportMonksApiClient.GetAllAsync<Region>(
                SportMonksApiRequest.Create("regions"),
                cancellationToken)).ToList();

            await _upsertService.UpsertAsync<Region, region>(regions);
        }
        private async Task ExecuteCities(CancellationToken cancellationToken)
        {
            var cities = (await _sportMonksApiClient.GetAllAsync<City>(
                SportMonksApiRequest.Create("cities"),
                cancellationToken)).ToList();

            await _upsertService.UpsertAsync<City, city>(cities);
        }
        private async Task ExecuteTypes(CancellationToken cancellationToken)
        {
            var typeList = (await _sportMonksApiClient.GetAllAsync<Types>(
                SportMonksApiRequest.Create("types"),
                cancellationToken)).ToList();

            await _upsertService.UpsertAsync<Types, types>(typeList);
        }

        private async Task ExecuteStates(CancellationToken cancellationToken)
        {
            var states = (await _sportMonksApiClient.GetAllAsync<State>(
                SportMonksApiRequest.Create("states"),
                cancellationToken)).ToList();

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
