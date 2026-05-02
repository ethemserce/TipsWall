using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using PreOddsApi.WebUI.Helper;
using PreOddsApi.WebUI.Models.Scorer.Models;
using PreOddsApi.WebUI.Models.Scorer.RequestModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.PrdApiServices
{
    public class TopScorerApiService
    {
        public static async Task<TopScorerViewModel> GetTopScorers(HttpContext context, long leagueId, long seasonId, long stageId, string api_url)
        {
            TopScorerRequestBodyModel jsonData = new TopScorerRequestBodyModel()
            {
                LeagueId = leagueId,
                SeasonId = seasonId,
                StageId = stageId,
                Language = CultureHandler.GetLocalLanguage(context.Request),
                ApiKey = ApiKeyHandler.GetApiKey()
            };

            string apiUrl = api_url + "topScorers";
            var jsonString = JsonConvert.SerializeObject(jsonData);

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.PostAsync(apiUrl, new StringContent(jsonString, Encoding.UTF8, "application/json"));
                    if (response.IsSuccessStatusCode)
                    {
                        return JsonConvert.DeserializeObject<TopScorerViewModel>(await response.Content.ReadAsStringAsync());
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
