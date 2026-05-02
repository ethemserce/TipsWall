using PreOddsApi.ExternalApis.SportMonks;

namespace PreOddsApi.Worker.SportMonks.Core
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                //var continents = await SportMonksApi.GetContinents(_configuration, _logger);

                //var countries = await SportMonksApi.GetCountries(_configuration, _logger);

                //var regions = await SportMonksApi.GetRegions(_configuration, _logger);

                //var cities = await SportMonksApi.GetCities(_configuration, _logger);

                //var types = await SportMonksApi.GetTypes(_configuration, _logger);
                //var type = await SportMonksApi.GetTypeByID(_configuration, _logger, 2);

                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}