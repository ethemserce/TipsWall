using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.League.V2Models
{
    public class LeagueListV2ViewModel
    {
        public LeagueListV2ViewModel()
        {
            this.Leagues = new List<LeagueV2ViewModel>();
        }
        public List<LeagueV2ViewModel> Leagues { get; set; }
    }
}
