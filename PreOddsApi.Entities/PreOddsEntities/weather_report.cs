using PreOddsApi.Core.Model;

namespace PreOddsApi.Entities.PreOddsEntities
{
    public class weather_report : BaseEntity
    {
        public long fixtureId { get; set; }
        public fixture fixture { get; set; }
        public long venueId { get; set; }
        public venue venue { get; set; }
        public string temperature { get; set; }
        public string feels_like { get; set; }
        public string wind { get; set; }
        public string humidity { get; set; }
        public int pressure { get; set; }
        public string clouds { get; set; }
        public string description { get; set; }
        public string icon { get; set; }
        public string type { get; set; }
        public string metric { get; set; }
        public string current { get; set; }
    }
}
