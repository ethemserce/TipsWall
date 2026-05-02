using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PreOddsApi.Entities.SportMonks.Football.Statistics.V3
{
    public class StatisticData
    {
        [JsonProperty("value")]
        public int Value { get; set; }
    }
}
