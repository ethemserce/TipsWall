using PreOddsApi.Core.Model;
using System;
using System.Collections.Generic;

namespace PreOddsApi.Entities.PreOddsEntities
{
    public class group : BaseEntity
    {
        public long? sportId { get; set; }
        public sport sport { get; set; }
        public long? leagueId { get; set; }
        public league league { get; set; }
        public long? seasonId { get; set; }
        public season season { get; set; }
        public long? stageId { get; set; }
        public stage stage { get; set; }
        public string name { get; set; }
        public DateTime? startingAt { get; set; }
        public DateTime? endingAt { get; set; }
        public ICollection<fixture> fixtures { get; set; }
    }
}
