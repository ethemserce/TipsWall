using PreOddsApi.BusinessLayer.Entities.BusinessEntities;

namespace PreOddsApi.WebUI.Models.Events.Models
{
    public class EventsBaseViewModel
    {
        public EventsBusinessModel LocalTeamEvent { get; set; }
        public EventsBusinessModel VisitorTeamEvent { get; set; }
        public string Type { get; set; }
        public string TypeName { get; set; }
        public long? TypeId { get; set; }
        public int Injuried { get; set; }
        public int Minute { get; set; }
        public int ExtraMinute { get; set; }
    }
}
