using PreOddsApi.Core.Model;
using System;

namespace PreOddsApi.Entities.PreOddsEntities
{
    public class prd_fixture_of_day : BaseEntity
    {
        public long fixtureId { get; set; }
        public DateTime? timeStartingAtDate { get; set; }
        public int flag { get; set; } = 1;
    }
}