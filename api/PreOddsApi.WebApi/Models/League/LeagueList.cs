using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.League
{
    public class LeagueList
    {
        public LeagueList()
        {
            this.Leagues = new List<LeagueViewModel>();
        }
        public List<LeagueViewModel> Leagues { get; set; }
    }
}
