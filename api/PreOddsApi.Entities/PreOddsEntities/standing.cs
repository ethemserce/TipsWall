using PreOddsApi.Core.Model;
using System;

namespace PreOddsApi.Entities.PreOddsEntities
{
  public  class standing : BaseEntity
    {
        public long? teamId { get; set; }
        public team team { get; set; }
        public long? sportId { get; set; }
        public sport sport { get; set; }
        public long? leagueId { get; set; }
        public league league { get; set; }
        public long? seasonId { get; set; }
        public season season { get; set; }
        public long? stageId { get; set; }
        public stage stage { get; set; }
        public long? groupId { get; set; }
        public group group { get; set; }
        public long? roundId { get; set; }
        public round round { get; set; }
        public long? standingRuleId { get; set; }
        public standing_rule standing_rule { get; set; }
        public int? position { get; set; }
        public string result { get; set; }
        public int? points { get; set; }
    }
}
