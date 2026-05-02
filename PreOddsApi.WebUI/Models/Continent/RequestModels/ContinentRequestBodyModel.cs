using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.Models.Continent.RequestModels
{
    public class ContinentRequestBodyModel
    {
        public string Language { get; set; }
        public string ApiKey { get; set; }
    }
}
