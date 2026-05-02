using PreOddsApi.Core.Model;

namespace PreOddsApi.Entities.PreOddsEntities
{
    public class tvstation : BaseEntity
    {
        public long? fixtureId { get; set; }
        public fixture fixture { get; set; }
        public string name { get; set; }
        public string url { get; set; }
        public string imagePath { get; set; }
    }
}
