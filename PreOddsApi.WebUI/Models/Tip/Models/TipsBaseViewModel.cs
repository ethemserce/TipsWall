using PreOddsApi.WebUI.Models.Continent.Models;
using PreOddsApi.WebUI.Models.Fixture.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.Models.Tip.Models
{
    public class TipsBaseViewModel
    {
        public int Page { get; set; }
        public bool IsLastPage { get; set; }
        public bool Success { get; set; }
        public List<TipsViewModel> Tips { get; set; }
        public List<FixtureForFixtureOfDayViewModel> FixtureOfDay { get; set; }
        public ContinentListViewModel Continents { get; set; }
    }
}
