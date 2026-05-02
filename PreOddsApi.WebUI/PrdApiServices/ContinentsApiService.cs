using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using PreOddsApi.WebUI.Helper;
using PreOddsApi.WebUI.Models.Continent.Models;
using PreOddsApi.WebUI.Models.Continent.RequestModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.PrdApiServices
{
    public static class ContinentsApiService
    {
        public static async Task<ContinentListViewModel> GetContinents(HttpContext context, string apiBaseUrl)
        {
            ContinentRequestBodyModel jsonData = new ContinentRequestBodyModel()
            {
                Language = CultureHandler.GetLocalLanguage(context.Request),
                ApiKey = ApiKeyHandler.GetApiKey()
            };

            string apiUrl = apiBaseUrl + "continents";
            var jsonString = JsonConvert.SerializeObject(jsonData);

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.PostAsync(apiUrl, new StringContent(jsonString, Encoding.UTF8, "application/json"));
                    if (response.IsSuccessStatusCode)
                    {
                        return JsonConvert.DeserializeObject<ContinentListViewModel>(await response.Content.ReadAsStringAsync());
                    }
                }
            }
            catch (Exception exc)
            {
                return null;
            }

            return null;
        }
    }
}
