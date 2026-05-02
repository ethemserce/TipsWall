using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.TopScorer.RequestModels
{
    public class TopScorerRequestBodyModel
    {
        public long LeagueId { get; set; }
        public long SeasonId { get; set; }
        public long StageId { get; set; }
        public string Language { get; set; }
        public string ApiKey { get; set; }
    }
}
