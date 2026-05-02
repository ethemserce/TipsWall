using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Helpers
{
    public static class OddsHandler
    {
        public static string FormatOdd(string odd)
        {
            if (odd.Length == 1 || odd.Length == 2)
            {
                odd = odd + ".00";
            }
            else if (odd.Length == 3)
            {
                odd = odd + "0";
            }

            return odd;
        }
    }
}
