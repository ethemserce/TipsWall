using PreOddsApi.WebUI.Models.Country.Models;
using PreOddsApi.WebUI.Models.Fixture.Models;
using PreOddsApi.WebUI.Models.Scorer.Models;
using PreOddsApi.WebUI.Models.Season.Models;
using PreOddsApi.WebUI.Models.Standing.Models;
using PreOddsApi.WebUI.Models.Statistic.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.Models.League.Models
{
    public class LeagueDetailBaseViewModel
    {
        public string Name { get; set; }
        public string Logo { get; set; }
        public CountryViewModel Country { get; set; } = new CountryViewModel();
        //public LeagueDetailViewModel LeagueDetail { get; set; }
        public List<SeasonViewModel> Seasons { get; set; } = new List<SeasonViewModel>();
        public List<FixtureOfRoundBaseViewModel> FixtureOfRounds { get; set; } = new List<FixtureOfRoundBaseViewModel>();
        public List<StandingViewModel> LeagueStanding { get; set; } = new List<StandingViewModel>();
        public TopScorerViewModel TopScorers { get; set; } = new TopScorerViewModel();
        public SeasonStatsViewModel SeasonStats { get; set; } = new SeasonStatsViewModel();
    }
}
