using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Statistic.RequestModels
{
    public class SeasonStatsRequestBodyModel
    {
        public long LeagueId { get; set; }
        public long SeasonId { get; set; }
        public string Language { get; set; }
        public string ApiKey { get; set; }
    }
}
