using PreOddsApi.Core.Model;

namespace PreOddsApi.Entities.PreOddsEntities
{
    public class comment : BaseEntity
    {
        public string text { get; set; }
        public int? extraMinute { get; set; }
        public long fixtureId { get; set; }
        public fixture fixture { get; set; }
        public int? goal { get; set; }
        public int? important { get; set; }
        public int? minute { get; set; }
        public int? order { get; set; }
    }
}
