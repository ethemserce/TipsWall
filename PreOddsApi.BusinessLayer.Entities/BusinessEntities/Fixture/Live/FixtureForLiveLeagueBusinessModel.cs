using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities.Fixture.Live
{
    public class FixtureForLiveLeagueBusinessModel
    {
        public FixtureForLiveLeagueBusinessModel()
        {
            this.Data = new List<FixtureForLiveItemBusinessModel>();
        }
        public long LeagueId { get; set; }
        public string LeagueName { get; set; }
        public long CountryId { get; set; }
        public string CountryName { get; set; }
        public string Logo { get; set; }
        public bool Favorite { get; set; }
        public List<FixtureForLiveItemBusinessModel> Data { get; set; }
    }
}
