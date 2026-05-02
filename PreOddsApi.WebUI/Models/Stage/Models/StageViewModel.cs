using PreOddsApi.WebUI.Models.Round.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.Models.Stage.Models
{
    public class StageViewModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public long SeasonId { get; set; }
        public string Type { get; set; }
        public List<RoundViewModel> Rounds { get; set; }
        public long GroupId { get; set; }
        public long CurrentStageId { get; set; }
    }
}
