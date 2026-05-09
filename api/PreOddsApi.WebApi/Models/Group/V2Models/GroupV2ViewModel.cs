using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Group.V2Models
{
    public class GroupV2ViewModel
    {
        public long Id { get; set; }
        //public DateTime CreateDateTime { get; set; }
        public string Name { get; set; }
        public long RoundId { get; set; }
        public long StageId { get; set; }
        //public DateTime UpdateDateTime { get; set; }
        //public List<RoundViewModel> Rounds { get; set; }
    }
}
