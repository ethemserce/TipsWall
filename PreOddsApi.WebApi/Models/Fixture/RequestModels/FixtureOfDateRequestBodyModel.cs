using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Fixture.RequestModels
{
    public class FixtureOfDateRequestBodyModel
    {
        public string Date { get; set; }
        public int TarihSecildimi { get; set; }
        public string Language { get; set; }
        public string TimeZone { get; set; }
        public string ApiKey { get; set; }
    }
}
