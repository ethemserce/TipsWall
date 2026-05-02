using PreOddsApi.Core.Model;
using System.Collections.Generic;

namespace PreOddsApi.Entities.PreOddsEntities
{
    public class aggregate : BaseEntity
    {
        public long leagueId { get; set; }
        public league league { get; set; }
        public long seasonId { get; set; }
        public season season { get; set; }
        public long stageId { get; set; }
        public stage stage { get; set; }
        public long teamId { get; set; }
        public team team { get; set; }
        public string name { get; set; }
        public long[] fixtureIds { get; set; }
        public string result { get; set; }
        public string detail { get; set; }
        public long winnerParticipantId { get; set; }
        public ICollection<fixture> fixtures { get; set; }
    }
}
