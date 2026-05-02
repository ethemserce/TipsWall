using PreOddsApi.WebApi.Models.League.V2Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Continent.V2Models
{
    public class ContinentListV2ViewModel
    {
        public ContinentListV2ViewModel()
        {
            this.Continents = new List<ContinentV2ViewModel>();
        }
        public List<ContinentV2ViewModel> Continents { get; set; }
        //public List<LeagueV2ViewModel> FavoriteLeagues { get; set; }
    }
}
