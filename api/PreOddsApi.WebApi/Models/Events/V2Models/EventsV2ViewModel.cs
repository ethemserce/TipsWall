using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Events.V2Models
{
    public class EventsV2ViewModel
    {
        public long Id { get; set; }
        public int ExtraMinute { get; set; }
        public long FixtureId { get; set; }
        public int Injuried { get; set; }
        public int Minute { get; set; }
        public long PlayerId { get; set; }
        public string PlayerName { get; set; }
        public string Reason { get; set; }
        public long RelatedPlayerId { get; set; }
        public string RelatedPlayerName { get; set; }
        public long TeamId { get; set; }
        public string Type { get; set; }
        public string TypeName { get; set; }
        public int Position { get; set; }
    }
}
