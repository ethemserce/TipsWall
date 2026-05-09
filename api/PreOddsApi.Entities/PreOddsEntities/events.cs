using PreOddsApi.Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.Entities.PreOddsEntities
{
    public class events : BaseEntity
    {
        public long? fixtureId { get; set; }
        public fixture fixture { get; set; }
        public long? periodId { get; set; }
        public period period { get; set; }
        public long? teamId { get; set; }
        public team team { get; set; }
        public long? typeId { get; set; }
        public types types { get; set; }
        public string section { get; set; }
        public long? playerId { get; set; }
        public player player { get; set; }
        public long? relatedPlayerId { get; set; }
        public player relatedPlayer { get; set; }
        public string playerName { get; set; }
        public string relatedPlayerName { get; set; }
        public string result { get; set; }
        public string info { get; set; }
        public string addition { get; set; }
        public int minute { get; set; }
        public int? extraMinute { get; set; }
        public bool? injuried { get; set; }
        public bool? onBench { get; set; }
        public long? coachId { get; set; }
        public coach coach { get; set; }
        public long? subTypeId { get; set; }
        public types subType { get; set; }
    }
}
