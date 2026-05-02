using PreOddsApi.Core.Model;

namespace PreOddsApi.Entities.PreOddsEntities
{
    public class news : BaseEntity
    {
        public long? fixtureId { get; set; }
        public fixture fixture { get; set; }
        public long? leagueId { get; set; }
        public league league { get; set; }
        public string title { get; set; }
        public string type { get; set; }
    }
}
