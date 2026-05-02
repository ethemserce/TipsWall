using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.Models.Odd.RequestModels
{
    public class FixtureOddsRequestBodyModel
    {
        public long FixtureId { get; set; }
        public long BookmarkerId { get; set; }
        public long MarketId { get; set; }
        public string Language { get; set; }
        public string ApiKey { get; set; }
    }
}
