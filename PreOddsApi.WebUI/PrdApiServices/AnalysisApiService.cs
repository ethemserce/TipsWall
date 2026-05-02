using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using PreOddsApi.WebUI.Helper;
using PreOddsApi.WebUI.Models.Analysis.Models;
using PreOddsApi.WebUI.Models.Analysis.RequestModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.PrdApiServices
{
    public static class AnalysisApiService
    {
        public static async Task<FixtureForOddAnalysisBaseViewModel> GetHotrates(HttpContext context, long bookmakerId, long markerId, string date, int winningPercente, int earningPercente, string analysisPeriod, string minRate, int matchState, int page, string api_url)
        {
            HotRatesRequestBodyModel jsonData = new HotRatesRequestBodyModel()
            {
                BookmakerId = bookmakerId,
                MarketId = markerId,
                Date = date,
                WinningPercente = winningPercente,
                EarningPercente = earningPercente,
                AnalysisPeriod = analysisPeriod,
                MinRate = minRate,
                MatchState = matchState,
                Page = page,
                Language = CultureHandler.GetLocalLanguage(context.Request),
                Timezone = CultureHandler.GetTimezone(context).ToString(),
                ApiKey = ApiKeyHandler.GetApiKey()
            };

            string apiUrl = api_url + "hotrateFixtures";
            var jsonString = JsonConvert.SerializeObject(jsonData);

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.PostAsync(apiUrl, new StringContent(jsonString, Encoding.UTF8, "application/json"));
                    if (response.IsSuccessStatusCode)
                    {
                        return JsonConvert.DeserializeObject<FixtureForOddAnalysisBaseViewModel>(await response.Content.ReadAsStringAsync());
                    }
                }
            }
            catch (Exception exc)
            {
                return null;
            }

            return null;
        }

        public static async Task<FixtureForOddAnalysisBaseViewModel> GetWinning(HttpContext context, long bookmarkerId, long markerId, string date, int winningPercente, string analysisPeriod, string minRate, int matchState, int page, string api_url)
        {
            WinningPercenteRequestBodyModel jsonData = new WinningPercenteRequestBodyModel()
            {
                BookmarkerId = bookmarkerId,
                MarketId = markerId,
                Date = date,
                WinningPercente = winningPercente,
                AnalysisPeriod = analysisPeriod,
                MinRate = minRate,
                MatchState = matchState,
                Page = page,
                Language = CultureHandler.GetLocalLanguage(context.Request),
                Timezone = CultureHandler.GetTimezone(context).ToString(),
                ApiKey = ApiKeyHandler.GetApiKey()
            };

            string apiUrl = api_url + "winningPercenteFixtures";
            var jsonString = JsonConvert.SerializeObject(jsonData);

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.PostAsync(apiUrl, new StringContent(jsonString, Encoding.UTF8, "application/json"));
                    if (response.IsSuccessStatusCode)
                    {
                        return JsonConvert.DeserializeObject<FixtureForOddAnalysisBaseViewModel>(await response.Content.ReadAsStringAsync());
                    }
                }
            }
            catch (Exception exc)
            {
                return null;
            }

            return null;
        }

        public static async Task<FixtureForOddAnalysisBaseViewModel> GetEarning(HttpContext context, long bookmarkerId, long markerId, string date, string analysisPeriod, string minRate, int matchState, int page,string api_url)
        {
            EarningPercenteRequestBodyModel jsonData = new EarningPercenteRequestBodyModel()
            {
                BookmarkerId = bookmarkerId,
                MarketId = markerId,
                Date = date,
                AnalysisPeriod = analysisPeriod,
                MinRate = minRate,
                MatchState = matchState,
                Page = page,
                Language = CultureHandler.GetLocalLanguage(context.Request),
                Timezone = CultureHandler.GetTimezone(context).ToString(),
                ApiKey = ApiKeyHandler.GetApiKey()
            };

            string apiUrl = api_url + "earningPercenteFixtures";
            var jsonString = JsonConvert.SerializeObject(jsonData);

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.PostAsync(apiUrl, new StringContent(jsonString, Encoding.UTF8, "application/json"));
                    if (response.IsSuccessStatusCode)
                    {
                        return JsonConvert.DeserializeObject<FixtureForOddAnalysisBaseViewModel>(await response.Content.ReadAsStringAsync());
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
