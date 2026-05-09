using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Concrete
{
    public static class CacheKeys
    {
        public static string HotRates => "HotRates_{0}_{1}_{2}";
        public static string Leagues => "Leagues_{0}";
        public static string Countries => "Countries_{0}";
        public static string Teams => "Teams_{0}";
        public static string Leagues_Live => "Leagues_{0}_{1}";
        public static string Countries_Live => "Countries_{0}_{1}";
        public static string Teams_Live => "Teams_{0}_{1}";
        public static string FixtureForLive => "FixtureForLive_{0}_{1}";
    }
}
