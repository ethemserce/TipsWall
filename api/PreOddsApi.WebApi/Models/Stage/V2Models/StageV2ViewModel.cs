using PreOddsApi.WebApi.Models.Round.V2Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Stage.V2Models
{
    public class StageV2ViewModel
    {
        public StageV2ViewModel()
        {
            this.Rounds = new List<RoundV2ViewModel>();
        }
        public long Id { get; set; }
        public string Name { get; set; }
        public long SeasonId { get; set; }
        public string Type { get; set; }
        public List<RoundV2ViewModel> Rounds { get; set; }
        public long GroupId { get; set; }
    }
}
