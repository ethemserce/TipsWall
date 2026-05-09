using PreOddsApi.Core.Model;

namespace PreOddsApi.Entities.PreOddsEntities
{
    public class standing_rule : BaseEntity
    {
        public string modelType { get; set; }
        public long? modelId { get; set; }
        public long? typeId { get; set; }
        public types types { get; set; }
        public string position { get; set; }
    }
}
