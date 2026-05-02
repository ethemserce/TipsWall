using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.TvStation.V2Models
{
    public class TvstationV2ViewModel
    {
        public long Id { get; set; }
        public long FixtureId { get; set; }
        public string Name { get; set; }
    }
}
