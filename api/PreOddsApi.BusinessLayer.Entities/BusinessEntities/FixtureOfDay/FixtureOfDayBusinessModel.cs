using System;

namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities.FixtureOfDay
{
    public class FixtureOfDayBusinessModel
    {
        public long Id { get; set; }
        public long FixtureId { get; set; }
        public DateTime TimeStartingAtDate { get; set; }
        public DateTime CreateDateTime { get; set; }
        public int Flag { get; set; }
        public FixtureForFixtureOfDayBusinessModel Fixture { get; set; }
    }
}
