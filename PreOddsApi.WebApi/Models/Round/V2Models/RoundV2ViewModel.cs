using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Round.V2Models
{
    public class RoundV2ViewModel
    {
        public long Id { get; set; }
        public string End { get; set; }
        public int Name { get; set; }
        public long StageId { get; set; }
        public string Start { get; set; }
    }
}
