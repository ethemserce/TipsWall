using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Corner.V2Models
{
    public class CornerV2ViewModel
    {
        public string Comment { get; set; }
        public int ExtraMinute { get; set; }
        //public long FixtureId { get; set; }
        public int Minute { get; set; }
        public long TeamId { get; set; }
    }
}
