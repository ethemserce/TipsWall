using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Fixture
{
    public class SeasonViewModel
    {
        public SeasonViewModel()
        {
            this.Stages = new List<StageViewModel>();
        }

        public long Id { get; set; }
        public DateTime CreateDateTime { get; set; }
        public long CurrentRoundId { get; set; }
        public bool CurrentSeason { get; set; }
        public long CurrentStageId { get; set; }
        public long LeagueId { get; set; }
        public string Name { get; set; }
        public DateTime UpdateDateTime { get; set; }
        public List<StageViewModel> Stages { get; set; }
    }
}
