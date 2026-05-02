using PreOddsApi.Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.Entities.PreOddsEntities
{
    public class goalscorer : BaseEntity
    {
        public long? leagueId { get; set; }
        public league league { get; set; }
        public long? playerId { get; set; }
        public player player { get; set; }
        public int? position { get; set; }
        public long? seasonId { get; set; }
        public season season { get; set; }
        public long? stageId { get; set; }
        public stage stage { get; set; }
        public long? teamId { get; set; }
        public team team { get; set; }
        public int? goals { get; set; }
        public int? penaltyGoals { get; set; }

    }
}
