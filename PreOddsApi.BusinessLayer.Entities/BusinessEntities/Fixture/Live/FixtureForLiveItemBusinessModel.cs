using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities.Fixture.Live
{
    public class FixtureForLiveItemBusinessModel
    {
        public long Id { get; set; }
        public string LocalTeamName { get; set; }
        public int? LocalTeamScore { get; set; }
        public string VisitorTeamName { get; set; }
        public int? VisitorTeamScore { get; set; }
        public string TimeStatus { get; set; }
        public string TimeStartingAtDate { get; set; }
        public bool Favorite { get; set; }
        public bool Live { get; set; }
    }
}
