using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.FixtureOfDay.RequestModels
{
    public class FixtureOfDayRequestBodyModel
    {
        public string Date { get; set; }
        public string Language { get; set; }
        public string TimeZone { get; set; }
        public string ApiKey { get; set; }
    }
}
