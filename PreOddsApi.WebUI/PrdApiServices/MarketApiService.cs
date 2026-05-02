using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using PreOddsApi.WebUI.Helper;
using PreOddsApi.WebUI.Models.Market.Models;
using PreOddsApi.WebUI.Models.Market.RequestModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.PrdApiServices
{
    public class MarketApiService
    {
        public static async Task<List<MarketViewModel>> GetMarkets(HttpContext context,string apiBaseUrl)
        {
            MarketRequestBodyModel jsonData = new MarketRequestBodyModel()
            {
                Language = CultureHandler.GetLocalLanguage(context.Request),
                ApiKey = ApiKeyHandler.GetApiKey()
            };

            string apiUrl = apiBaseUrl + "markets";
            var jsonString = JsonConvert.SerializeObject(jsonData);

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.PostAsync(apiUrl, new StringContent(jsonString, Encoding.UTF8, "application/json"));
                    if (response.IsSuccessStatusCode)
                    {
                        return JsonConvert.DeserializeObject<List<MarketViewModel>>(await response.Content.ReadAsStringAsync());
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
