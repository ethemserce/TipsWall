using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Odds.V2Models
{
    public class OddWinLostSeriesV2ViewModel
    {
        public string OddSeries { get; set; }
        public string LocalTeamSeries { get; set; }
        public string VisitorTeamSeries { get; set; }
        public string LeagueSeries { get; set; }
    }
}
