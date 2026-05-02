using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Odds.RequestModels
{
    public class FixtureOddsRequestBodyModel
    {
        public long FixtureId { get; set; }
        public long bookmaker_id { get; set; }
        public long MarketId { get; set; }
        public string Language { get; set; }
        public string ApiKey { get; set; }
    }
}
