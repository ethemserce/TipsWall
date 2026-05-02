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
        private readonly IConfiguration _configuration;
        private readonly IUpsertService<PreOddsApiDbContext> _upsertService;

        public CoreWorkerService(ILogger<CoreWorkerService> logger,
                IConfiguration configuration,
                IUpsertService<PreOddsApiDbContext> upsertService)
        {
            _logger = logger;
            _configuration = configuration;
            _upsertService = upsertService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Core Service execution started!");
            try
            {
                ExecuteCoreData();

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

        private async void ExecuteCoreData()
        {
            Dictionary<string, string> queryParameters = new Dictionary<string, string>();

            queryParameters.Add("include", "countries");
            var continents = await SportMonksApi.GetAll<Continent>(_configuration, _logger, "continents", queryParameters);

            Dictionary<string, string> queryParameters1 = new Dictionary<string, string>();
            queryParameters1.Add("include", "regions.cities");
            var countries = await SportMonksApi.GetAll<Country>(_configuration, _logger, "countries", queryParameters1);

            foreach (var continent in continents)
            {
                foreach (var country in continent.Countries)
                {
                    country.Regions.AddRange(countries.FirstOrDefault(x => x.Id == country.Id).Regions);
                }
            }

            var jsonData = JsonConvert.SerializeObject(continents);

            await _upsertService.UpsertAsync<Continent, continent>(continents);
        }

        private async void ExecuteContinents()
        {
            await _upsertService.UpsertAsync<Continent, continent>(await SportMonksApi.GetAll<Continent>(_configuration, _logger, "continents"));
        }

        private async void ExecuteCountries()
        {
            await _upsertService.UpsertAsync<Country, country>(await SportMonksApi.GetAll<Country>(_configuration, _logger, "countries"));
        }

        private async void ExecuteRegions()
        {
            await _upsertService.UpsertAsync<Region, region>(await SportMonksApi.GetAll<Region>(_configuration, _logger, "regions"));
        }
        private async void ExecuteCities()
        {
            await _upsertService.UpsertAsync<City, city>(await SportMonksApi.GetAll<City>(_configuration, _logger, "cities"));
        }
        private async void ExecuteTypes()
        {
            await _upsertService.UpsertAsync<Types, types>(await SportMonksApi.GetAll<Types>(_configuration, _logger, "types"));
        }

        private async void ExecuteStates()
        {
            await _upsertService.UpsertAsync<State, state>(await SportMonksApi.GetAll<State>(_configuration, _logger, "states"));
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
