using PreOddsApi.WebApi.Models.League;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Continent
{
    public class ContinentList
    {
        public List<ContinentViewModel> Continents { get; set; }
        public List<LeagueViewModel> FavoriteLeagues { get; set; }
    }
}
