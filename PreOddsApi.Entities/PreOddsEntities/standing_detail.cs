using PreOddsApi.Core.Model;

namespace PreOddsApi.Entities.PreOddsEntities
{
    public class standing_detail: BaseEntity
    {
        public string standingType { get; set; }
        public long? standingId { get; set; }
        public standing standing { get; set; }
        public long? typeId { get; set; }
        public types types { get; set; }
        public int? value { get; set; }
    }
}
