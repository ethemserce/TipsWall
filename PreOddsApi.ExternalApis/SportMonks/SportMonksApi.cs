using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PreOddsApi.Entities.PreOddsEntities;
using PreOddsApi.Entities.SportMonks;
using PreOddsApi.Entities.SportMonks.Core.V3;
using PreOddsApi.Entities.SportMonks.Football.V3;
using PreOddsApi.ExternalApis.ValueObjects;
using RestSharp;
using System.Collections.Generic;
using System.Xml.Linq;

namespace PreOddsApi.ExternalApis.SportMonks
{
    public static class SportMonksApi
    {
        public async static Task<List<T>> GetAll<T>(IConfiguration configuration, ILogger logger, string apiUrl, Dictionary<string, string> queryParameters = null)
        {
            return await GetBaseTAsync<List<T>>(configuration, logger, apiUrl, queryParameters);
        }

        public async static Task<List<T>> GetAll<T>(IConfiguration configuration, ILogger logger, string apiUrl, long id, Dictionary<string, string> queryParameters = null)
        {
            return await GetBaseTAsync<List<T>>(configuration, logger, apiUrl + id, queryParameters);
        }

        public async static Task<List<T>> GetAll<T>(IConfiguration configuration, ILogger logger, string apiUrl, string name, Dictionary<string, string> queryParameters = null)
        {
            return await GetBaseTAsync<List<T>>(configuration, logger, apiUrl + name, queryParameters);
        }

        public async static Task<T> Get<T>(IConfiguration configuration, ILogger logger, string apiUrl, Dictionary<string, string> queryParameters = null)
        {
            //string response = await BaseRequestManager(configuration, logger, "cities", new Dictionary<string, string>());

            //var sportMonksBaseData = GetObject<SportMonksBase<List<City>>>(response);

            //return sportMonksBaseData.Data;

            return await GetBaseTAsync<T>(configuration, logger, apiUrl, queryParameters);
        }

        public async static Task<T> Get<T>(IConfiguration configuration, ILogger logger, string apiUrl,long id, Dictionary<string, string> queryParameters = null)
        {
            //string response = await BaseRequestManager(configuration, logger, "cities", new Dictionary<string, string>());

            //var sportMonksBaseData = GetObject<SportMonksBase<List<City>>>(response);

            //return sportMonksBaseData.Data;

            return await GetBaseTAsync<T>(configuration, logger, apiUrl + id, queryParameters);
        }

        public async static Task<T> Get<T>(IConfiguration configuration, ILogger logger, string apiUrl, string name, Dictionary<string, string> queryParameters = null)
        {
            //string response = await BaseRequestManager(configuration, logger, "cities", new Dictionary<string, string>());

            //var sportMonksBaseData = GetObject<SportMonksBase<List<City>>>(response);

            //return sportMonksBaseData.Data;

            return await GetBaseTAsync<T>(configuration, logger, apiUrl + name, queryParameters);
        }

        private async static Task<SportMonksBase<T>> BaseRequestManager<T>(IConfiguration configuration, ILogger logger, string api_url, Dictionary<string, string> queryParameters = null)
        {
            logger.LogInformation("Base Request Manager Begin at: {time}", DateTimeOffset.Now);

            var sportMonksValues = configuration.GetSection("SportMonksValues").Get<SportMonksValues>();

            string requestUrl = api_url.Split('/')[0].ToLower() == "https:" ? api_url : "/" + sportMonksValues?.version + "/" + sportMonksValues?.sport + "/" + api_url;

            var request = new RestRequest(requestUrl, Method.Get);
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Authorization", sportMonksValues?.api_key);

            request.AddQueryParameter("per_page", "50");
            if (queryParameters != null)
            {
                foreach (var item in queryParameters)
                {
                    request.AddQueryParameter(item.Key, item.Value);
                }
            }

            var options = new RestClientOptions()
            {
                BaseUrl = new Uri(sportMonksValues?.api_baseUrl)
            };

            var client = new RestClient(options);
            try
            {
                RestResponse response = await client.ExecuteAsync(request);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return GetObject<SportMonksBase<T>>(response.Content.ToString());
                }


                return null;
            }
            catch (Exception exc)
            {

                throw;
            }
        }

        private async static Task<T> GetBaseTAsync<T>(IConfiguration configuration, ILogger logger, string api_url, Dictionary<string, string> queryParameters = null)
        {
            dynamic response = await BaseRequestManager<T>(configuration, logger, api_url, queryParameters);

            bool hasMorePage = response.Pagination == null ? false : response.Pagination.HasMore;
            string nextPage = "";
            if (hasMorePage)
            {
                nextPage = response.Pagination == null ? "" : response.Pagination.NextPage.ToString();
            }

            while (hasMorePage)
            {
                var nextPageData = await BaseRequestManager<T>(configuration, logger, nextPage, queryParameters);

                response.Data.AddRange(nextPageData.Data);

                hasMorePage = nextPageData.Pagination.HasMore;
                if (hasMorePage)
                {
                    nextPage = nextPageData.Pagination.NextPage.ToString();
                }
            }

            return response.Data;
        }

        private static T GetObject<T>(string? response) => JsonConvert.DeserializeObject<T>(response);
    }
}
