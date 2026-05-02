using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Standing.RequestModels
{
    public class TeamStandingRequestBodyModel
    {
        public long TeamId { get; set; }
        public string Language { get; set; }
        public string ApiKey { get; set; }
    }
}
