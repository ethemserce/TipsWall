using System;

namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities
{
    public class EventsBusinessModel
    {
        public long Id { get; set; }
        public DateTime CreateDateTime { get; set; }
        public int ExtraMinute { get; set; }
        public long FixtureId { get; set; }
        public int Injuried { get; set; }
        public int Minute { get; set; }
        public long PlayerId { get; set; }
        public string PlayerName { get; set; }
        //public PlayerBusinessModel Player { get; set; }
        public string Reason { get; set; }
        public long RelatedPlayerId { get; set; }
        public string RelatedPlayerName { get; set; }
        //public PlayerBusinessModel RelatedPlayer { get; set; }
        public long TeamId { get; set; }
        public long? TypeId { get; set; }
        public string? Type { get; set; }
        public string? TypeName { get; set; }
        public int Position { get; set; }
        public DateTime UpdateDateTime { get; set; }
    }
}
