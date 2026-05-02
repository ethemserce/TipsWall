using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Helpers
{
    public static class ApiKeyHandler
    {
        public static string GetApiKey()
        {
            return Environment.GetEnvironmentVariable("PREODDS_INTERNAL_API_KEY") ?? "CHANGE_ME_INTERNAL_API_KEY";
        }
    }
}
