using PreOddsApi.Core.Model;

namespace PreOddsApi.Entities.PreOddsEntities
{
    public class score : BaseEntity
    {
        public long fixtureId { get; set; }
        public fixture fixture { get; set; }
        public long typeId { get; set; }
        public types types { get; set; }
        public long teamId { get; set; }
        public team team { get; set; }
        public string description { get; set; }
        public int goals { get; set; }
        public string location { get; set; }

    }
}
