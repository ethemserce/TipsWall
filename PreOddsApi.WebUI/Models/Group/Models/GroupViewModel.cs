using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.Models.Group.Models
{
    public class GroupViewModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public long RoundId { get; set; }
        public long StageId { get; set; }
    }
}
