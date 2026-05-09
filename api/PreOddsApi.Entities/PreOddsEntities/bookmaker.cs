using PreOddsApi.Core.Model;

namespace PreOddsApi.Entities.PreOddsEntities
{
    public class bookmaker : BaseEntity
    {
        public long? legacy_id { get; set; }
        public string name { get; set; }
    }
}
