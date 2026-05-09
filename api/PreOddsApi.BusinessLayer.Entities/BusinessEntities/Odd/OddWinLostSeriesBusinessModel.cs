using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities.Odd
{
    public class OddWinLostSeriesBusinessModel
    {
        public string OddSeries { get; set; } = "";
        public string LocalTeamSeries { get; set; } = "";
        public string VisitorTeamSeries { get; set; } = "";
        public string LeagueSeries { get; set; } = "";
    }
}
