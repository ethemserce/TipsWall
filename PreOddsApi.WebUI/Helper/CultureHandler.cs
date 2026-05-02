using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.Helper
{
    public class CultureHandler
    {
        public static string GetLocalLanguage(HttpRequest request)
        {
            var language = request.Headers["Accept-Language"];
            if(string.IsNullOrEmpty(language))
            {
                language = "en-US";
            }
            return CheckLanguage(language);
        }

        public static double GetTimezone(HttpContext context)
        {
            double timezone = 0;
            if (double.TryParse(context.Session.GetString("timezone"), out timezone))
            {
                return timezone;
            }
            return 0;
        }

        private static string CheckLanguage(string lang)
        {
            switch (lang.Split(',')[0])
            {
                case "tr-TR": return "tr-TR";
                case "tr": return "tr-TR";
                case "TR": return "tr-TR";
                case "en-GB": return "en-GB";
                case "en-US": return "en-US";
                default:
                    return "en-US";
            }
        }
    }
}
