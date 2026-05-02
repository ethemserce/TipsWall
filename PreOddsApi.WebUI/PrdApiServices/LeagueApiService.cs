using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using PreOddsApi.WebUI.Helper;
using PreOddsApi.WebUI.Models.League.Models;
using PreOddsApi.WebUI.Models.League.RequestModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.PrdApiServices
{
    public static class LeagueApiService
    {
        public static async Task<LeagueListViewModel> GetLeagues(HttpContext context, long countryId, string api_url)
        {
            LeagueRequestBodyModel jsonData = new LeagueRequestBodyModel()
            {
                CountryId = countryId,
                Language = CultureHandler.GetLocalLanguage(context.Request),
                ApiKey = ApiKeyHandler.GetApiKey()
            };

            string apiUrl = api_url + "leagues";
            var jsonString = JsonConvert.SerializeObject(jsonData);

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.PostAsync(apiUrl, new StringContent(jsonString, Encoding.UTF8, "application/json"));
                    if (response.IsSuccessStatusCode)
                    {
                        return JsonConvert.DeserializeObject<LeagueListViewModel>(await response.Content.ReadAsStringAsync());
                    }
                }
            }
            catch (Exception exc)
            {
                return null;
            }

            return null;
        }

        public static async Task<LeagueDetailBaseViewModel> GetLeagueDetail(HttpContext context, long leagueId, string api_url)
        {
            LeagueDetailRequestBodyModel jsonData = new LeagueDetailRequestBodyModel()
            {
                LeagueId = leagueId,
                Language = CultureHandler.GetLocalLanguage(context.Request),
                ApiKey = ApiKeyHandler.GetApiKey()
            };

            string apiUrl = api_url + "leagueDetail";
            var jsonString = JsonConvert.SerializeObject(jsonData);

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.PostAsync(apiUrl, new StringContent(jsonString, Encoding.UTF8, "application/json"));
                    if (response.IsSuccessStatusCode)
                    {
                        return JsonConvert.DeserializeObject<LeagueDetailBaseViewModel>(await response.Content.ReadAsStringAsync());
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
