using PreOddsApi.Core.Model;
using System;

namespace PreOddsApi.Entities.PreOddsEntities
{
    public class topScorer : BaseEntity
    {
        public long stageId { get; set; }
        public stage stage { get; set; }
        public long playerId { get; set; }
        public player player { get; set; }
        public long typeId { get; set; }
        public types types { get; set; }
        public int position { get; set; }
        public int total { get; set; }
        public string participantType { get; set; }
        public long teamId { get; set; }
        public team team { get; set; }
    }
}
