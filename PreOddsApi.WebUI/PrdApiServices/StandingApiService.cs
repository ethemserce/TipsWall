using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using PreOddsApi.WebUI.Helper;
using PreOddsApi.WebUI.Models.Standing.Models;
using PreOddsApi.WebUI.Models.Standing.RequestModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.PrdApiServices
{
    public static class StandingApiService
    {
        public static async Task<StandingViewModel> GetTeamStanding(HttpContext context, long teamId)
        {
            TeamStandingRequestBodyModel jsonData = new TeamStandingRequestBodyModel()
            {
                TeamId = teamId,
                Language = CultureHandler.GetLocalLanguage(context.Request),
                ApiKey = ApiKeyHandler.GetApiKey()
            };

            string apiUrl = "https://prdapi.preodds.com/api/teamStandings";
            var jsonString = JsonConvert.SerializeObject(jsonData);

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.PostAsync(apiUrl, new StringContent(jsonString, Encoding.UTF8, "application/json"));
                    if (response.IsSuccessStatusCode)
                    {
                        return JsonConvert.DeserializeObject<StandingViewModel>(await response.Content.ReadAsStringAsync());
                    }
                }
            }
            catch (Exception exc)
            {
                return null;
            }

            return null;
        }

        public static async Task<List<StandingViewModel>> GetLeagueStanding(HttpContext context, long leagueId, long seasonId, long stageId, long groupId, string api_url)
        {
            LeagueStandingRequestBodyModel jsonData = new LeagueStandingRequestBodyModel()
            {
                LeagueId = leagueId,
                SeasonId = seasonId,
                StageId = stageId,
                GroupId = groupId,
                Language = CultureHandler.GetLocalLanguage(context.Request),
                ApiKey = ApiKeyHandler.GetApiKey()
            };

            string apiUrl = api_url + "leagueStandings";
            var jsonString = JsonConvert.SerializeObject(jsonData);

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.PostAsync(apiUrl, new StringContent(jsonString, Encoding.UTF8, "application/json"));
                    if (response.IsSuccessStatusCode)
                    {
                        return JsonConvert.DeserializeObject<List<StandingViewModel>>(await response.Content.ReadAsStringAsync());
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
