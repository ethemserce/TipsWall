using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.Models.Analysis.RequestModels
{
    public class HotRatesRequestBodyModel
    {
        public long BookmakerId { get; set; }
        public long MarketId { get; set; }
        public string Date { get; set; }
        public int WinningPercente { get; set; }
        public int EarningPercente { get; set; }
        public string AnalysisPeriod { get; set; }
        public string MinRate { get; set; }
        public int MatchState { get; set; }
        public int Page { get; set; }
        public string Language { get; set; }
        public string Timezone { get; set; }
        public string ApiKey { get; set; }
    }
}
