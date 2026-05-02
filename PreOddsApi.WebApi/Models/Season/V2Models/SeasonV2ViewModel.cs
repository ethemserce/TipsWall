using PreOddsApi.WebApi.Models.Stage.V2Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Season.V2Models
{
    public class SeasonV2ViewModel
    {
        public long Id { get; set; }
        public long CurrentRoundId { get; set; }
        public bool CurrentSeason { get; set; }
        public long CurrentStageId { get; set; }
        public long LeagueId { get; set; }
        public string Name { get; set; }
        public List<StageV2ViewModel> Stages { get; set; } = new List<StageV2ViewModel>();
    }
}
