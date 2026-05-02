using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using PreOddsApi.WebUI.Helper;
using PreOddsApi.WebUI.Models.Tip.Models;
using PreOddsApi.WebUI.Models.Tip.RequestModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.PrdApiServices
{
    public static class TipsApiServices
    {
        public static async Task<TipsBaseViewModel> GetTips(HttpContext context, int page, string apiBaseUrl)
        {
            TipsRequestBodyModel jsonData = new TipsRequestBodyModel()
            {
                Page = page,
                Language = CultureHandler.GetLocalLanguage(context.Request),
                TimeZone = CultureHandler.GetTimezone(context).ToString(),
                ApiKey = ApiKeyHandler.GetApiKey()
            };

            //string apiUrl = "https://prdapi.preodds.com/api/tips";

            string apiUrl = apiBaseUrl + "tips";
            var jsonString = JsonConvert.SerializeObject(jsonData);

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.PostAsync(apiUrl, new StringContent(jsonString, Encoding.UTF8, "application/json"));
                    if (response.IsSuccessStatusCode)
                    {
                        return JsonConvert.DeserializeObject<TipsBaseViewModel>(await response.Content.ReadAsStringAsync());
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
