using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Helpers
{
    public static class ApiLanguageHandler
    {
        public static string GetLanguage(string lang)
        {
            switch (lang)
            {
                case "en-GB": return lang;
                case "en-US": return lang;
                case "tr-TR": return lang;
                default: return "en-GB";
            }
        }
    }
}
