using PreOddsApi.Core.Model;

namespace PreOddsApi.Entities.PreOddsEntities
{
    public class formation : BaseEntity
    {
        public long? fixtureId { get; set; }
        public fixture fixture { get; set; }
        public long? teamId { get; set; }
        public team team { get; set; }
        public string teamFormation { get; set; }
        public string location { get; set; }
    }
}