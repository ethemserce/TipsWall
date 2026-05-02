using PreOddsApi.WebUI.Models.Stage.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.Models.Season.Models
{
    public class SeasonViewModel
    {
        public long Id { get; set; }
        public long CurrentRoundId { get; set; }
        public bool CurrentSeason { get; set; }
        public long CurrentStageId { get; set; }
        public long LeagueId { get; set; }
        public string Name { get; set; }
        public List<StageViewModel> Stages { get; set; }
    }
}
