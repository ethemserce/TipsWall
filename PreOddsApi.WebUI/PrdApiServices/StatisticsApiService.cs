using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using PreOddsApi.WebUI.Helper;
using PreOddsApi.WebUI.Models.Analysis.Models;
using PreOddsApi.WebUI.Models.Analysis.RequestModels;
using PreOddsApi.WebUI.Models.Statistic.Models;
using PreOddsApi.WebUI.Models.Statistic.RequestModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.PrdApiServices
{
    public class StatisticsApiService
    {
        public static async Task<SeasonStatsViewModel> GetSeasonStats(HttpContext context, long leagueId, long seasonId, string api_url)
        {
            SeasonStatsRequestBodyModel jsonData = new SeasonStatsRequestBodyModel()
            {
                LeagueId = leagueId,
                SeasonId = seasonId,
                Language = CultureHandler.GetLocalLanguage(context.Request),
                ApiKey = ApiKeyHandler.GetApiKey()
            };

            string apiUrl = api_url + "seasonStats";
            var jsonString = JsonConvert.SerializeObject(jsonData);

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.PostAsync(apiUrl, new StringContent(jsonString, Encoding.UTF8, "application/json"));
                    if (response.IsSuccessStatusCode)
                    {
                        return JsonConvert.DeserializeObject<SeasonStatsViewModel>(await response.Content.ReadAsStringAsync());
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
