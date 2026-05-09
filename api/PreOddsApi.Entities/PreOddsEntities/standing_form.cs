using PreOddsApi.Core.Model;

namespace PreOddsApi.Entities.PreOddsEntities
{
    public class standing_form : BaseEntity
    {
        public string standingType { get; set; }
        public long? standingId { get; set; }
        public standing standing { get; set; }
        public long? fixtureId { get; set; }
        public fixture fixture { get; set; }
        public string form { get; set; }
        public int sortOrder { get; set; }
    }
}
