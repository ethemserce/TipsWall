using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities.Fixture
{
    public class FixtureForFavoriteBusinessModel
    {
        public FixtureForFavoriteBusinessModel()
        {
            this.Fixture = new List<FixtureForLeagueBusinessModel>();
        }
        public List<FixtureForLeagueBusinessModel> Fixture { get; set; }
    }
}
