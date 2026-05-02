using PreOddsApi.Core.Model;
namespace PreOddsApi.Entities.PreOddsEntities
{
    public class statistic : BaseEntity
    {
        public long? fixtureId { get; set; }
        public fixture fixture { get; set; }
        public long? teamId { get; set; }
        public team team { get; set; }
        public long? typeId { get; set; }
        public types types { get; set; }
        public int? value { get; set; }
        public string location { get; set; }
    }
}
