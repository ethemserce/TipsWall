using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Fixture
{
    public class StageViewModel
    {
        public StageViewModel()
        {
            this.Rounds = new List<RoundViewModel>();
        }
        public long Id { get; set; }
        public DateTime CreateDateTime { get; set; }
        public string Name { get; set; }
        public long SeasonId { get; set; }
        public string Type { get; set; }
        public DateTime UpdateDateTime { get; set; }
        public List<RoundViewModel> Rounds { get; set; }
        //public List<GroupViewModel> Groups { get; set; }
        public long GroupId { get; set; }
    }
}
