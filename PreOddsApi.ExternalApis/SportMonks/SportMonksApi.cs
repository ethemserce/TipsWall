using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PreOddsApi.ExternalApis.SportMonks
{
    public static class SportMonksApi
    {
        public static Task<List<T>> GetAll<T>(
            IConfiguration configuration,
            ILogger logger,
            string apiUrl,
            Dictionary<string, string>? queryParameters = null)
        {
            return GetAllAsync<T>(configuration, logger, apiUrl, queryParameters);
        }

        public static Task<List<T>> GetAll<T>(
            IConfiguration configuration,
            ILogger logger,
            string apiUrl,
            long id,
            Dictionary<string, string>? queryParameters = null)
        {
            return GetAllAsync<T>(configuration, logger, $"{apiUrl}{id}", queryParameters);
        }

        public static Task<List<T>> GetAll<T>(
            IConfiguration configuration,
            ILogger logger,
            string apiUrl,
            string name,
            Dictionary<string, string>? queryParameters = null)
        {
            return GetAllAsync<T>(configuration, logger, $"{apiUrl}{name}", queryParameters);
        }

        public static Task<T> Get<T>(
            IConfiguration configuration,
            ILogger logger,
            string apiUrl,
            Dictionary<string, string>? queryParameters = null)
        {
            return GetAsync<T>(configuration, logger, apiUrl, queryParameters);
        }

        public static Task<T> Get<T>(
            IConfiguration configuration,
            ILogger logger,
            string apiUrl,
            long id,
            Dictionary<string, string>? queryParameters = null)
        {
            return GetAsync<T>(configuration, logger, $"{apiUrl}{id}", queryParameters);
        }

        public static Task<T> Get<T>(
            IConfiguration configuration,
            ILogger logger,
            string apiUrl,
            string name,
            Dictionary<string, string>? queryParameters = null)
        {
            return GetAsync<T>(configuration, logger, $"{apiUrl}{name}", queryParameters);
        }

        private static async Task<List<T>> GetAllAsync<T>(
            IConfiguration configuration,
            ILogger logger,
            string apiUrl,
            Dictionary<string, string>? queryParameters)
        {
            using var httpClient = new HttpClient();
            var client = CreateClient(configuration, logger, httpClient);
            var request = CreateRequest(apiUrl, queryParameters);
            var data = await client.GetAllAsync<T>(request);

            return data.ToList();
        }

        private static async Task<T> GetAsync<T>(
            IConfiguration configuration,
            ILogger logger,
            string apiUrl,
            Dictionary<string, string>? queryParameters)
        {
            using var httpClient = new HttpClient();
            var client = CreateClient(configuration, logger, httpClient);
            var request = CreateRequest(apiUrl, queryParameters);

            return await client.GetDataAsync<T>(request);
        }

        private static SportMonksApiClient CreateClient(
            IConfiguration configuration,
            ILogger logger,
            HttpClient httpClient)
        {
            var options = SportMonksApiOptions.FromConfiguration(configuration);
            return new SportMonksApiClient(httpClient, options, logger);
        }

        private static SportMonksApiRequest CreateRequest(
            string apiUrl,
            Dictionary<string, string>? queryParameters)
        {
            return SportMonksApiRequest.Create(apiUrl)
                .WithQueryParameters(queryParameters);
        }
    }
}
