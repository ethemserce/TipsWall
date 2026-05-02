using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.Models.Standing.RequestModels
{
    public class LeagueStandingRequestBodyModel
    {
        public long LeagueId { get; set; }
        public long SeasonId { get; set; }
        public long StageId { get; set; }
        public long GroupId { get; set; }
        public string Language { get; set; }
        public string ApiKey { get; set; }
    }
}
