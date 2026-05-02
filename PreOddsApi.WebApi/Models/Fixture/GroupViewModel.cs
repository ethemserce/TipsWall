using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Fixture
{
    public class GroupViewModel
    {
        public long Id { get; set; }
        public DateTime CreateDateTime { get; set; }
        public string Name { get; set; }
        public long RoundId { get; set; }
        public long StageId { get; set; }
        public DateTime UpdateDateTime { get; set; }
        public List<RoundViewModel> Rounds { get; set; }
    }
}
