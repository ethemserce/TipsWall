using PreOddsApi.Core.Model;
namespace PreOddsApi.Entities.PreOddsEntities
{
    public class sidelined : BaseEntity
    {
        public long? fixtureId { get; set; }
        public fixture fixture { get; set; }
        public long? playerId { get; set; }
        public player player { get; set; }
        public long? typeId { get; set; }
        public types types { get; set; }
        public long? teamId { get; set; }
        public team team { get; set; }
        public long? seasonId { get; set; }
        public season season { get; set; }
        public string category { get; set; }
        public string startDate { get; set; }
        public string endDate { get; set; }
        public int? gamesMissed { get; set; }
        public bool completed { get; set; }
    }
}
