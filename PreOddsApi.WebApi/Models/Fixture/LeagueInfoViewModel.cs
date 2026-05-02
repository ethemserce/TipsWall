using PreOddsApi.WebApi.Models.League;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Fixture
{
    public class LeagueInfoViewModel
    {
        public LeagueViewModel League { get; set; }
        public List<SeasonViewModel> Seasons { get; set; }
        public List<StageViewModel> Stages { get; set; }
        public List<RoundViewModel> Rounds { get; set; }
        public List<GroupViewModel> Groups { get; set; }
    }
}
