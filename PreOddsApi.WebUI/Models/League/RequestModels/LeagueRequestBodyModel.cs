using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.Models.League.RequestModels
{
    public class LeagueRequestBodyModel
    {
        public long CountryId { get; set; }
        public string Language { get; set; }
        public string ApiKey { get; set; }
    }
}
