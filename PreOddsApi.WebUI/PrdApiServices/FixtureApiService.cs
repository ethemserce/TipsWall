using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using PreOddsApi.WebApi.Models.Fixture.V2Models;
using PreOddsApi.WebApi.Models.Market.V2Models;
using PreOddsApi.WebUI.Helper;
using PreOddsApi.WebUI.Models.Fixture.Models;
using PreOddsApi.WebUI.Models.Fixture.RequestModels;
using PreOddsApi.WebUI.Models.Odd.Models;
using PreOddsApi.WebUI.Models.Odd.RequestModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.PrdApiServices
{
    public class FixtureApiService
    {
        public static async Task<List<FixtureOfRoundBaseViewModel>> GetFixtureOfRound(HttpContext context, long leagueId, long seasonId, long stageId, long groupId, long roundId, string api_url)
        {
            FixtureOfRoundRequestBodyModel jsonData = new FixtureOfRoundRequestBodyModel()
            {
                LeagueId = leagueId,
                SeasonId = seasonId,
                StageId = stageId,
                GroupId = groupId,
                RoundId = roundId,
                TimeZone = CultureHandler.GetTimezone(context).ToString(),
                Language = CultureHandler.GetLocalLanguage(context.Request),
                ApiKey = ApiKeyHandler.GetApiKey()
            };

            string apiUrl = api_url +"fixtureOfRound";
            var jsonString = JsonConvert.SerializeObject(jsonData);

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.PostAsync(apiUrl, new StringContent(jsonString, Encoding.UTF8, "application/json"));
                    if (response.IsSuccessStatusCode)
                    {
                        return JsonConvert.DeserializeObject<List<FixtureOfRoundBaseViewModel>>(await response.Content.ReadAsStringAsync());
                    }
                }
            }
            catch (Exception exc)
            {
                return null;
            }

            return null;
        }

        public static async Task<FixtureV2ViewModel> GetFixture(HttpContext context, long fixtureId, string api_url)
        {
            FixtureRequestBodyModel jsonData = new FixtureRequestBodyModel()
            {
                FixtureId = fixtureId,
                TimeZone = CultureHandler.GetTimezone(context).ToString(),
                Language = CultureHandler.GetLocalLanguage(context.Request),
                ApiKey = ApiKeyHandler.GetApiKey()
            };

            string apiUrl = api_url + "fixture";
            var jsonString = JsonConvert.SerializeObject(jsonData);

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.PostAsync(apiUrl, new StringContent(jsonString, Encoding.UTF8, "application/json"));
                    if (response.IsSuccessStatusCode)
                    {
                        return JsonConvert.DeserializeObject<FixtureV2ViewModel>(await response.Content.ReadAsStringAsync());
                        
                    }
                }
            }
            catch (Exception exc)
            {
                return null;
            }

            return null;
        }

        public static async Task<FixtureForLiveViewModel> GetFixtureOfDate(HttpContext context, string date, int isDateSelected, string api_url)
        {
            FixtureOfDateRequestBodyModel jsonData = new FixtureOfDateRequestBodyModel()
            {
                Date = date,
                TarihSecildimi = isDateSelected,
                TimeZone = CultureHandler.GetTimezone(context).ToString(),
                Language = CultureHandler.GetLocalLanguage(context.Request),
                ApiKey = ApiKeyHandler.GetApiKey()
            };

            string apiUrl = api_url + "fixtureOfDate";
            var jsonString = JsonConvert.SerializeObject(jsonData);


            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.PostAsync(apiUrl, new StringContent(jsonString, Encoding.UTF8, "application/json"));
                    if (response.IsSuccessStatusCode)
                    {
                        return JsonConvert.DeserializeObject<FixtureForLiveViewModel>(await response.Content.ReadAsStringAsync());
                    }
                }
            }
            catch (Exception exc)
            {
                return null;
            }

            return null;
        }

        public static async Task<List<MarketForOddsV2ViewModel>> GetFixtureOdds(HttpContext context, long fixtureId, long marketId, string api_url)
        {
            FixtureOddsRequestBodyModel jsonData = new FixtureOddsRequestBodyModel()
            {
                FixtureId = fixtureId,
                BookmarkerId = 2,
                MarketId = marketId,
                Language = CultureHandler.GetLocalLanguage(context.Request),
                ApiKey = ApiKeyHandler.GetApiKey()
            };

            string apiUrl = api_url + "fixtureOdds";
            var jsonString = JsonConvert.SerializeObject(jsonData);

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.PostAsync(apiUrl, new StringContent(jsonString, Encoding.UTF8, "application/json"));
                    if (response.IsSuccessStatusCode)
                    {
                        return JsonConvert.DeserializeObject<List<MarketForOddsV2ViewModel>>(await response.Content.ReadAsStringAsync());
                    }
                }
            }
            catch (Exception exc)
            {
                return null;
            }

            return null;
        }

        public static async Task<List<FixtureForFixtureOfDayViewModel>> GetTopFixtureOfDay(HttpContext context, string date, string apiBaseUrl)
        {
            FixtureOfDayRequestBodyModel jsonData = new FixtureOfDayRequestBodyModel()
            {
                Date = date,
                TimeZone = CultureHandler.GetTimezone(context).ToString(),
                Language = CultureHandler.GetLocalLanguage(context.Request),
                ApiKey = ApiKeyHandler.GetApiKey()
            };

            string apiUrl = apiBaseUrl + "TopFixtureOfDay";
            var jsonString = JsonConvert.SerializeObject(jsonData);

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.PostAsync(apiUrl, new StringContent(jsonString, Encoding.UTF8, "application/json"));
                    if (response.IsSuccessStatusCode)
                    {
                        return JsonConvert.DeserializeObject<List<FixtureForFixtureOfDayViewModel>>(await response.Content.ReadAsStringAsync());
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
