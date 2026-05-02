using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.League.RequestModels
{
    public class LeagueDetailInfoRequestBodyModel
    {
        public long LeagueId { get; set; }
        public string Language { get; set; }
        public string ApiKey { get; set; }
    }
}
