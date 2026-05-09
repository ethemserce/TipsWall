using PreOddsApi.Core.Model;
using System;
using System.Collections.Generic;

namespace PreOddsApi.Entities.PreOddsEntities
{
   public class stage : BaseEntity
    {
        public long sportId { get; set; }
        public sport sport { get; set; }
        public long leagueId { get; set; }
        public league league { get; set; }
        public long seasonId { get; set; }
        public season season { get; set; }
        public long typeId { get; set; }
        public types types { get; set; }
        public string name { get; set; }
        public int sortOrder { get; set; }
        public bool finished { get; set; }
        public bool isCurrent { get; set; }
        public DateTime? startingAt { get; set; }
        public DateTime? endingAt { get; set; }
        public bool gamesInCurrentWeek { get; set; }
        public long? tieBreakerRuleId { get; set; }
        public ICollection<round> rounds { get; set; }
    }
}
