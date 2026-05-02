using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Country.RequestModels
{
    public class CountryRequestBodyModel
    {
        public string Language { get; set; }
        public string ApiKey { get; set; }
    }
}
