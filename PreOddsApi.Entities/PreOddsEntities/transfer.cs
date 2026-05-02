using PreOddsApi.Core.Model;
using System;

namespace PreOddsApi.Entities.PreOddsEntities
{
    public class transfer : BaseEntity
    {
        public long sportId { get; set; }
        public sport sport { get; set; }
        public long playerId { get; set; }
        public player player { get; set; }
        public long typeId { get; set; }
        public types types { get; set; }
        public long fromTeamTd { get; set; }
        public team fromTeam { get; set; }
        public long toTeamId { get; set; }
        public team toTeam { get; set; }
        public int positionId { get; set; }
        public int detailedPositionId { get; set; }
        public DateOnly date { get; set; }
        public bool career_ended { get; set; }
        public bool completed { get; set; }
        public long amount { get; set; }
    }
}
