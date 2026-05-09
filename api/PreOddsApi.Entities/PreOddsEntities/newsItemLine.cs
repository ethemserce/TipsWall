using PreOddsApi.Core.Model;

namespace PreOddsApi.Entities.PreOddsEntities
{
    public class newsItemLine : BaseEntity
    {
        public long newsitemId { get; set; }
        public news news { get; set; }
        public string text { get; set; }
        public string type { get; set; }
    }
}
