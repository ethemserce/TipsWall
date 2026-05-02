using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.Models.Round.Models
{
    public class RoundViewModel
    {
        public long Id { get; set; }
        public string End { get; set; }
        public int Name { get; set; }
        public long StageId { get; set; }
        public string Start { get; set; }
        public long CurrentRoundId { get; set; }
    }
}
