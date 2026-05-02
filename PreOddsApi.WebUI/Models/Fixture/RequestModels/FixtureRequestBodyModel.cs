using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.Models.Fixture.RequestModels
{
    public class FixtureRequestBodyModel
    {
        public long FixtureId { get; set; }
        public string Language { get; set; }
        public string TimeZone { get; set; }
        public string ApiKey { get; set; }
    }
}
