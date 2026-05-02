using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.Models.Tvstation.Models
{
    public class TvstationViewModel
    {
        public long Id { get; set; }
        public long FixtureId { get; set; }
        public string Name { get; set; }
    }
}
